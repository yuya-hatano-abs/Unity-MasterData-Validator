using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// Task 3 固定構造 JSON のパースと records 配列の取得を行います。
    /// </summary>
    internal static class MasterJsonDocumentReader
    {
        /// <summary>
        /// JSON ルートをパースします。
        /// </summary>
        internal static bool TryParseRoot(string json, IList<ImportIssue> issues, out JToken root)
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

        /// <summary>
        /// ルートから records 配列を取得します。
        /// </summary>
        internal static bool TryGetRecordsArray(JToken root, IList<ImportIssue> issues, out JArray records)
        {
            records = null;

            if (root.Type == JTokenType.Array)
            {
                issues.Add(new ImportIssue(
                    ImportSeverity.Error,
                    ImportIssueKind.MissingRecordsArray,
                    "ルートはオブジェクトで \"records\" 配列を含む必要があります。例: { \"records\": [ ... ] }",
                    -1,
                    null,
                    GetLineNumber(root)));
                return false;
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
                return false;
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
                return false;
            }

            records = (JArray)recordsToken;
            return true;
        }

        internal static bool TryValidateDuplicatePropertyNames(string json, IList<ImportIssue> issues)
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

        internal static int GetLineNumber(JToken token)
        {
            if (token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return lineInfo.LineNumber;
            }

            return 0;
        }
    }
}
