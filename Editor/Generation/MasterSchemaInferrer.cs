using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using Newtonsoft.Json.Linq;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// JSON 文字列からマスタのスキーマを推論します。
    /// </summary>
    public static class MasterSchemaInferrer
    {
        static readonly Regex FieldNamePattern = new Regex("^[a-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
        static readonly HashSet<string> ReservedPropertyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Id",
            "IdKind",
            "Key",
            "Equals",
            "GetHashCode",
            "GetType",
            "ToString",
            "MemberwiseClone",
            "Finalize",
            "ReferenceEquals",
        };

        /// <summary>
        /// JSON からスキーマを推論します。
        /// </summary>
        /// <param name="json">JSON 文字列です。</param>
        /// <returns>推論結果です。</returns>
        public static SchemaInferenceResult Infer(string json)
        {
            var issues = new List<GenerationIssue>();
            var importIssues = new List<ImportIssue>();

            if (json == null)
            {
                json = string.Empty;
            }

            JToken root;
            if (!MasterJsonDocumentReader.TryParseRoot(json, importIssues, out root))
            {
                AppendSourceErrors(importIssues, issues);
                return Failed(issues);
            }

            JArray records;
            if (!MasterJsonDocumentReader.TryGetRecordsArray(root, importIssues, out records))
            {
                AppendSourceErrors(importIssues, issues);
                return Failed(issues);
            }

            if (records.Count == 0)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.EmptyRecords,
                    "records 配列が空のため、型を推論できません。",
                    null,
                    MasterJsonDocumentReader.GetLineNumber(records)));
                return Failed(issues);
            }

            var fieldOrder = new List<string>();
            var fieldStates = new Dictionary<string, FieldInferenceState>(StringComparer.Ordinal);

            MasterIdKind? idKind = null;
            var idError = false;

            for (var recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                var item = records[recordIndex];
                if (item.Type != JTokenType.Object)
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.SourceError,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, null, "レコードは JSON オブジェクトである必要があります。"),
                        null,
                        MasterJsonDocumentReader.GetLineNumber(item)));
                    idError = true;
                    continue;
                }

                var recordObject = (JObject)item;
                if (!TryInferIdKind(recordObject, recordIndex, issues, ref idKind))
                {
                    idError = true;
                }

                foreach (var property in recordObject.Properties())
                {
                    if (string.Equals(property.Name, "id", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!fieldStates.ContainsKey(property.Name))
                    {
                        fieldOrder.Add(property.Name);
                        fieldStates[property.Name] = new FieldInferenceState(property.Name);
                    }

                    fieldStates[property.Name].AddObservation(property.Value);
                }
            }

            if (idError || idKind == null || HasError(issues))
            {
                return Failed(issues);
            }

            var schemaFields = new List<SchemaField>();
            for (var i = 0; i < fieldOrder.Count; i++)
            {
                var jsonName = fieldOrder[i];
                if (!ValidateFieldName(jsonName, issues))
                {
                    continue;
                }

                var propertyName = ToPropertyName(jsonName);
                if (ReservedPropertyNames.Contains(propertyName))
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.ReservedFieldName,
                        "キー \"" + jsonName + "\" は予約済みのプロパティ名 \"" + propertyName + "\" と衝突します。",
                        jsonName,
                        0));
                    continue;
                }

                SchemaField field;
                if (!TryBuildSchemaField(jsonName, fieldStates[jsonName], issues, out field))
                {
                    continue;
                }

                schemaFields.Add(field);
            }

            if (HasError(issues))
            {
                return Failed(issues);
            }

            var schema = new MasterSchema(idKind.Value, schemaFields);
            return new SchemaInferenceResult(schema, issues, succeeded: true);
        }

        static bool TryInferIdKind(JObject recordObject, int recordIndex, IList<GenerationIssue> issues, ref MasterIdKind? idKind)
        {
            JToken idToken;
            if (!recordObject.TryGetValue("id", out idToken))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.MissingId,
                    JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "id キーがありません。"),
                    "id",
                    MasterJsonDocumentReader.GetLineNumber(recordObject)));
                return false;
            }

            if (idToken.Type == JTokenType.Null)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InconsistentIdType,
                    JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "id キーの型がレコード間で一致しません。"),
                    "id",
                    MasterJsonDocumentReader.GetLineNumber(idToken)));
                return false;
            }

            if (idToken.Type == JTokenType.Integer)
            {
                long value;
                if (!JsonTokenConverter.TryReadIntegerTokenValue(idToken, out value))
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.IdOutOfRange,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "整数 id が int の範囲外です。"),
                        "id",
                        MasterJsonDocumentReader.GetLineNumber(idToken)));
                    return false;
                }

                if (value < int.MinValue || value > int.MaxValue)
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.IdOutOfRange,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "整数 id が int の範囲外です。"),
                        "id",
                        MasterJsonDocumentReader.GetLineNumber(idToken)));
                    return false;
                }

                if (idKind == null)
                {
                    idKind = MasterIdKind.Int;
                }
                else if (idKind.Value != MasterIdKind.Int)
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.InconsistentIdType,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "id キーの型がレコード間で一致しません。"),
                        "id",
                        MasterJsonDocumentReader.GetLineNumber(idToken)));
                    return false;
                }

                return true;
            }

            if (idToken.Type == JTokenType.String)
            {
                if (idKind == null)
                {
                    idKind = MasterIdKind.String;
                }
                else if (idKind.Value != MasterIdKind.String)
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.InconsistentIdType,
                        JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "id キーの型がレコード間で一致しません。"),
                        "id",
                        MasterJsonDocumentReader.GetLineNumber(idToken)));
                    return false;
                }

                return true;
            }

            issues.Add(new GenerationIssue(
                ImportSeverity.Error,
                GenerationIssueKind.InconsistentIdType,
                JsonTokenConverter.FormatRecordFieldMessage(recordIndex, "id", "id キーの型がレコード間で一致しません。"),
                "id",
                MasterJsonDocumentReader.GetLineNumber(idToken)));
            return false;
        }

        static bool ValidateFieldName(string jsonName, IList<GenerationIssue> issues)
        {
            if (FieldNamePattern.IsMatch(jsonName) && !CSharpKeywords.IsKeyword(jsonName))
            {
                return true;
            }

            var message = "キー \"" + jsonName + "\" は命名規則に適合しません。キーは英小文字で始めてください。";
            if (string.Equals(jsonName, "ID", StringComparison.Ordinal))
            {
                message += " ID は \"id\" と書いてください。";
            }
            else if (jsonName.Length > 0 && char.IsUpper(jsonName[0]))
            {
                message += " \"" + jsonName + "\" のように大文字で始めるキーは使用できません。";
            }

            issues.Add(new GenerationIssue(
                ImportSeverity.Error,
                GenerationIssueKind.InvalidFieldName,
                message,
                jsonName,
                0));
            return false;
        }

        static bool TryBuildSchemaField(
            string jsonName,
            FieldInferenceState state,
            IList<GenerationIssue> issues,
            out SchemaField field)
        {
            field = null;
            FieldShape shape;
            SchemaFieldType scalarType;
            if (!state.TryInfer(out shape, out scalarType, jsonName, issues))
            {
                return false;
            }

            if (shape == FieldShape.Unsupported)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Warning,
                    GenerationIssueKind.UnsupportedFieldValue,
                    "キー \"" + jsonName + "\" の値はサポートされていないため、フィールドを生成しません。",
                    jsonName,
                    0));
                return false;
            }

            var propertyName = ToPropertyName(jsonName);
            var isList = shape == FieldShape.List;
            var cSharpTypeName = isList
                ? "List<" + GetScalarTypeName(scalarType) + ">"
                : GetScalarTypeName(scalarType);

            field = new SchemaField(jsonName, scalarType, isList, propertyName, cSharpTypeName);
            return true;
        }

        static string GetScalarTypeName(SchemaFieldType type)
        {
            switch (type)
            {
                case SchemaFieldType.Int:
                    return "int";
                case SchemaFieldType.Long:
                    return "long";
                case SchemaFieldType.Float:
                    return "float";
                case SchemaFieldType.Bool:
                    return "bool";
                default:
                    return "string";
            }
        }

        static string ToPropertyName(string jsonName)
        {
            if (string.IsNullOrEmpty(jsonName))
            {
                return jsonName;
            }

            if (jsonName.Length == 1)
            {
                return jsonName.ToUpperInvariant();
            }

            return char.ToUpperInvariant(jsonName[0]) + jsonName.Substring(1);
        }

        static void AppendSourceErrors(IList<ImportIssue> importIssues, IList<GenerationIssue> issues)
        {
            for (var i = 0; i < importIssues.Count; i++)
            {
                var importIssue = importIssues[i];
                issues.Add(new GenerationIssue(
                    importIssue.Severity,
                    GenerationIssueKind.SourceError,
                    importIssue.Message,
                    importIssue.FieldName,
                    importIssue.LineNumber));
            }
        }

        static SchemaInferenceResult Failed(List<GenerationIssue> issues)
        {
            return new SchemaInferenceResult(null, issues, succeeded: false);
        }

        static bool HasError(IList<GenerationIssue> issues)
        {
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == ImportSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        enum ScalarKind
        {
            None,
            Int,
            Long,
            Float,
            Bool,
            String,
            Null,
            Object,
            Array,
            Inconsistent,
        }

        enum FieldShape
        {
            Scalar,
            List,
            Unsupported,
            Inconsistent,
        }

        sealed class FieldInferenceState
        {
            ScalarKind scalarKind = ScalarKind.None;
            bool sawArrayToken;
            bool sawNonEmptyArray;
            ScalarKind listElementKind = ScalarKind.None;

            public FieldInferenceState(string fieldName)
            {
            }

            public void AddObservation(JToken token)
            {
                if (token == null || token.Type == JTokenType.Null)
                {
                    scalarKind = MergeScalar(scalarKind, ScalarKind.Null);
                    return;
                }

                if (token.Type == JTokenType.Array)
                {
                    sawArrayToken = true;
                    var array = (JArray)token;
                    if (array.Count == 0)
                    {
                        return;
                    }

                    sawNonEmptyArray = true;
                    for (var i = 0; i < array.Count; i++)
                    {
                        listElementKind = MergeListElement(listElementKind, ClassifyScalar(array[i]));
                    }

                    return;
                }

                if (token.Type == JTokenType.Object)
                {
                    scalarKind = ScalarKind.Object;
                    return;
                }

                scalarKind = MergeScalar(scalarKind, ClassifyScalar(token));
            }

            public bool TryInfer(out FieldShape shape, out SchemaFieldType scalarType, string jsonName, IList<GenerationIssue> issues)
            {
                shape = FieldShape.Scalar;
                scalarType = SchemaFieldType.String;

                if (sawArrayToken)
                {
                    if (scalarKind != ScalarKind.None && scalarKind != ScalarKind.Null)
                    {
                        issues.Add(new GenerationIssue(
                            ImportSeverity.Error,
                            GenerationIssueKind.InconsistentFieldType,
                            "キー \"" + jsonName + "\" の型がレコード間で一致しません。",
                            jsonName,
                            0));
                        shape = FieldShape.Inconsistent;
                        return false;
                    }

                    if (!sawNonEmptyArray)
                    {
                        shape = FieldShape.List;
                        scalarType = SchemaFieldType.String;
                        return true;
                    }

                    if (listElementKind == ScalarKind.Inconsistent)
                    {
                        issues.Add(new GenerationIssue(
                            ImportSeverity.Error,
                            GenerationIssueKind.InconsistentFieldType,
                            "キー \"" + jsonName + "\" の配列要素の型がレコード間で一致しません。",
                            jsonName,
                            0));
                        shape = FieldShape.Inconsistent;
                        return false;
                    }

                    if (listElementKind == ScalarKind.Object || listElementKind == ScalarKind.Array)
                    {
                        shape = FieldShape.Unsupported;
                        return true;
                    }

                    shape = FieldShape.List;
                    scalarType = ToSchemaFieldType(listElementKind);
                    return true;
                }

                if (scalarKind == ScalarKind.Inconsistent)
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.InconsistentFieldType,
                        "キー \"" + jsonName + "\" の型がレコード間で一致しません。",
                        jsonName,
                        0));
                    shape = FieldShape.Inconsistent;
                    return false;
                }

                if (scalarKind == ScalarKind.Object || scalarKind == ScalarKind.Array)
                {
                    shape = FieldShape.Unsupported;
                    return true;
                }

                if (scalarKind == ScalarKind.None || scalarKind == ScalarKind.Null)
                {
                    shape = FieldShape.Scalar;
                    scalarType = SchemaFieldType.String;
                    return true;
                }

                shape = FieldShape.Scalar;
                scalarType = ToSchemaFieldType(scalarKind);
                return true;
            }

            static ScalarKind ClassifyScalar(JToken token)
            {
                if (token == null || token.Type == JTokenType.Null)
                {
                    return ScalarKind.Null;
                }

                if (token.Type == JTokenType.Boolean)
                {
                    return ScalarKind.Bool;
                }

                if (token.Type == JTokenType.String)
                {
                    return ScalarKind.String;
                }

                if (token.Type == JTokenType.Integer)
                {
                    long value;
                    if (!JsonTokenConverter.TryReadIntegerTokenValue(token, out value))
                    {
                        return ScalarKind.Object;
                    }

                    if (value < int.MinValue || value > int.MaxValue)
                    {
                        return ScalarKind.Long;
                    }

                    return ScalarKind.Int;
                }

                if (token.Type == JTokenType.Float)
                {
                    return ScalarKind.Float;
                }

                if (token.Type == JTokenType.Array)
                {
                    return ScalarKind.Array;
                }

                if (token.Type == JTokenType.Object)
                {
                    return ScalarKind.Object;
                }

                return ScalarKind.Object;
            }

            static ScalarKind MergeScalar(ScalarKind current, ScalarKind next)
            {
                if (next == ScalarKind.Null)
                {
                    return current == ScalarKind.None ? ScalarKind.Null : current;
                }

                if (current == ScalarKind.None || current == ScalarKind.Null)
                {
                    return next;
                }

                if (current == ScalarKind.Inconsistent || next == ScalarKind.Inconsistent)
                {
                    return ScalarKind.Inconsistent;
                }

                if (current == next)
                {
                    return current;
                }

                if ((current == ScalarKind.Int || current == ScalarKind.Long) && next == ScalarKind.Float)
                {
                    return ScalarKind.Float;
                }

                if (current == ScalarKind.Float && (next == ScalarKind.Int || next == ScalarKind.Long))
                {
                    return ScalarKind.Float;
                }

                if ((current == ScalarKind.Int && next == ScalarKind.Long) ||
                    (current == ScalarKind.Long && next == ScalarKind.Int))
                {
                    return ScalarKind.Long;
                }

                return ScalarKind.Inconsistent;
            }

            static ScalarKind MergeListElement(ScalarKind current, ScalarKind next)
            {
                if (next == ScalarKind.Null)
                {
                    return current;
                }

                if (current == ScalarKind.None || current == ScalarKind.Null)
                {
                    return next;
                }

                return MergeScalar(current, next);
            }

            static SchemaFieldType ToSchemaFieldType(ScalarKind kind)
            {
                switch (kind)
                {
                    case ScalarKind.Int:
                        return SchemaFieldType.Int;
                    case ScalarKind.Long:
                        return SchemaFieldType.Long;
                    case ScalarKind.Float:
                        return SchemaFieldType.Float;
                    case ScalarKind.Bool:
                        return SchemaFieldType.Bool;
                    default:
                        return SchemaFieldType.String;
                }
            }
        }
    }
}
