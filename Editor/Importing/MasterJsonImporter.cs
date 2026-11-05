using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MasterDataTool.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// 固定構造の JSON をマスタレコード一覧に変換します。
    /// </summary>
    public static class MasterJsonImporter
    {
        /// <summary>
        /// JSON 文字列をインポートします。
        /// </summary>
        /// <typeparam name="TRecord">レコード型です。</typeparam>
        /// <param name="json">JSON 文字列です。</param>
        /// <returns>インポート結果です。</returns>
        public static ImportResult<TRecord> Import<TRecord>(string json)
            where TRecord : MasterRecordBase
        {
            if (json == null)
            {
                json = string.Empty;
            }

            return ImportCore<TRecord>(json);
        }

        /// <summary>
        /// UTF-8 ファイル(BOM あり・なし)から JSON をインポートします。
        /// </summary>
        /// <typeparam name="TRecord">レコード型です。</typeparam>
        /// <param name="filePath">ファイルパスです。</param>
        /// <returns>インポート結果です。</returns>
        public static ImportResult<TRecord> ImportFromFile<TRecord>(string filePath)
            where TRecord : MasterRecordBase
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return CreateFileNotFoundResult<TRecord>(filePath);
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    return CreateFileNotFoundResult<TRecord>(filePath);
                }

                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return ImportCore<TRecord>(json);
            }
            catch (Exception)
            {
                return CreateFileNotFoundResult<TRecord>(filePath);
            }
        }

        static ImportResult<TRecord> ImportCore<TRecord>(string json)
            where TRecord : MasterRecordBase
        {
            var issues = new List<ImportIssue>();
            var recordType = typeof(TRecord);

            if (recordType.IsAbstract)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.UnsupportedRecordType,
                    "抽象型 '" + recordType.Name + "' はインポートできません。",
                    -1,
                    null,
                    0));
                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            var fieldMap = SerializedFieldMap.Get(recordType);
            if (fieldMap.DuplicateFieldNames.Count > 0)
            {
                for (var i = 0; i < fieldMap.DuplicateFieldNames.Count; i++)
                {
                    var duplicateName = fieldMap.DuplicateFieldNames[i];
                    issues.Add(new ImportIssue(
                        ImportSeverity.Error,
                        ImportIssueKind.DuplicateFieldName,
                        "レコード型 '" + recordType.Name + "' のシリアライズフィールド名 '" + duplicateName + "' が継承チェーン内で重複しています。",
                        -1,
                        duplicateName,
                        0));
                }

                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            JToken root;
            if (!TryParseRoot(json, issues, out root))
            {
                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            if (root.Type == JTokenType.Array)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.MissingRecordsArray,
                    "ルートはオブジェクトで \"records\" 配列を含む必要があります。例: { \"records\": [ ... ] }",
                    -1,
                    null,
                    GetLineNumber(root)));
                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            if (root.Type != JTokenType.Object)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.MissingRecordsArray,
                    "ルートに \"records\" 配列が見つかりません。",
                    -1,
                    null,
                    GetLineNumber(root)));
                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            var recordsToken = root["records"];
            if (recordsToken == null || recordsToken.Type != JTokenType.Array)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.MissingRecordsArray,
                    "ルートに \"records\" 配列が見つかりません。",
                    -1,
                    null,
                    GetLineNumber(root)));
                return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
            }

            var records = new List<TRecord>();
            var unsupportedWarnings = new HashSet<string>(StringComparer.Ordinal);
            var array = (JArray)recordsToken;

            for (var index = 0; index < array.Count; index++)
            {
                var item = array[index];
                if (item.Type != JTokenType.Object)
                {
                    issues.Add(new ImportIssue(
                        ImportSeverity.Error,
                        ImportIssueKind.RecordNotObject,
                        JsonTokenConverter.FormatRecordFieldMessage(index, null, "レコードは JSON オブジェクトである必要があります。"),
                        index,
                        null,
                        GetLineNumber(item)));
                    continue;
                }

                var record = CreateRecordInstance<TRecord>();
                ApplyRecordObject((JObject)item, record, fieldMap, index, issues, unsupportedWarnings);
                JsonTokenConverter.NormalizeRecord(record, fieldMap);
                records.Add(record);
            }

            return new ImportResult<TRecord>(records, issues, succeeded: true);
        }

        static void ApplyRecordObject(
            JObject recordObject,
            object record,
            SerializedFieldMap fieldMap,
            int recordIndex,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued)
        {
            var fieldByName = new Dictionary<string, SerializedFieldEntry>(StringComparer.Ordinal);
            for (var i = 0; i < fieldMap.Fields.Count; i++)
            {
                var entry = fieldMap.Fields[i];
                fieldByName[entry.Name] = entry;
            }

            var presentKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in recordObject.Properties())
            {
                presentKeys.Add(property.Name);

                SerializedFieldEntry entry;
                if (!fieldByName.TryGetValue(property.Name, out entry))
                {
                    issues.Add(new ImportIssue(
                        ImportSeverity.Warning,
                        ImportIssueKind.UnknownField,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, property.Name, "未知のフィールドです。"),
                        recordIndex,
                        property.Name,
                        GetLineNumber(property.Value)));
                    continue;
                }

                JsonTokenConverter.TryApplyFieldValue(
                    record,
                    entry,
                    property.Value,
                    recordIndex,
                    issues,
                    unsupportedFieldWarningsIssued);
            }

            for (var i = 0; i < fieldMap.Fields.Count; i++)
            {
                var entry = fieldMap.Fields[i];
                if (presentKeys.Contains(entry.Name))
                {
                    continue;
                }

                issues.Add(new ImportIssue(
                    ImportSeverity.Warning,
                    ImportIssueKind.MissingField,
                    JsonTokenConverter.FormatRecordFieldMessage(recordIndex, entry.Name, "フィールドに対応する JSON キーがありません。"),
                    recordIndex,
                    entry.Name,
                    0));
            }
        }

        static TRecord CreateRecordInstance<TRecord>()
            where TRecord : MasterRecordBase
        {
            var type = typeof(TRecord);
            var ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);

            if (ctor != null)
            {
                return (TRecord)ctor.Invoke(null);
            }

            return (TRecord)RuntimeHelpers.GetUninitializedObject(type);
        }

        static bool TryParseRoot(string json, IList<ImportIssue> issues, out JToken root)
        {
            root = null;

            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    issues.Add(new ImportIssue(
                        ImportSeverity.Error,
                        ImportIssueKind.InvalidJson,
                        "JSON が空です。",
                        -1,
                        null,
                        0));
                    return false;
                }

                if (!TryValidateDuplicatePropertyNames(json, issues))
                {
                    return false;
                }

                var loadSettings = new JsonLoadSettings
                {
                    LineInfoHandling = LineInfoHandling.Load,
                    CommentHandling = CommentHandling.Ignore,
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                };

                using (var stringReader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    jsonReader.DateParseHandling = DateParseHandling.None;
                    root = JToken.Load(jsonReader, loadSettings);

                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.Comment)
                        {
                            continue;
                        }

                        issues.Add(new ImportIssue(
                            ImportSeverity.Error,
                            ImportIssueKind.InvalidJson,
                            "JSON の末尾に余分なトークンがあります。",
                            -1,
                            null,
                            jsonReader.LineNumber));
                        root = null;
                        return false;
                    }
                }

                return true;
            }
            catch (JsonReaderException ex)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.InvalidJson,
                    "JSON の構文が不正です: " + ex.Message,
                    -1,
                    null,
                    ex.LineNumber));
                return false;
            }
            catch (JsonException ex)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.InvalidJson,
                    "JSON の構文が不正です: " + ex.Message,
                    -1,
                    null,
                    0));
                return false;
            }
        }

        static bool TryValidateDuplicatePropertyNames(string json, IList<ImportIssue> issues)
        {
            var propertyStacks = new Stack<HashSet<string>>();

            try
            {
                using (var stringReader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    jsonReader.DateParseHandling = DateParseHandling.None;
                    while (jsonReader.Read())
                    {
                        switch (jsonReader.TokenType)
                        {
                            case JsonToken.StartObject:
                                propertyStacks.Push(new HashSet<string>(StringComparer.Ordinal));
                                break;
                            case JsonToken.PropertyName:
                                if (propertyStacks.Count == 0)
                                {
                                    break;
                                }

                                var name = jsonReader.Value as string;
                                if (string.IsNullOrEmpty(name))
                                {
                                    break;
                                }

                                var set = propertyStacks.Peek();
                                if (!set.Add(name))
                                {
                                    issues.Add(new ImportIssue(
                                        ImportSeverity.Error,
                                        ImportIssueKind.InvalidJson,
                                        "JSON オブジェクト内に重複したキーがあります: " + name,
                                        -1,
                                        null,
                                        jsonReader.LineNumber));
                                    return false;
                                }

                                break;
                            case JsonToken.EndObject:
                                if (propertyStacks.Count > 0)
                                {
                                    propertyStacks.Pop();
                                }

                                break;
                        }
                    }
                }

                return true;
            }
            catch (JsonReaderException ex)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.InvalidJson,
                    "JSON の構文が不正です: " + ex.Message,
                    -1,
                    null,
                    ex.LineNumber));
                return false;
            }
        }

        static ImportResult<TRecord> CreateFileNotFoundResult<TRecord>(string filePath)
            where TRecord : MasterRecordBase
        {
            var pathText = string.IsNullOrEmpty(filePath) ? "(null)" : filePath;
            var issues = new List<ImportIssue>
            {
                new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.FileNotFound,
                    "ファイルを読み込めません: " + pathText,
                    -1,
                    null,
                    0)
            };
            return new ImportResult<TRecord>(Array.Empty<TRecord>(), issues, succeeded: false);
        }

        static int GetLineNumber(JToken token)
        {
            if (token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return lineInfo.LineNumber;
            }

            return 0;
        }
    }
}
