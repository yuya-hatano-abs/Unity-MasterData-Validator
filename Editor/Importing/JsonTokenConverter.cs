using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// JSON トークンをシリアライズ対象フィールドの型へ変換します。
    /// </summary>
    internal static class JsonTokenConverter
    {
        /// <summary>
        /// フィールドに JSON 値を設定します。変換できない場合は既定値のまま Issue を追加します。
        /// </summary>
        public static void TryApplyFieldValue(
            object record,
            SerializedFieldEntry fieldEntry,
            JToken token,
            int recordIndex,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued)
        {
            var field = fieldEntry.FieldInfo;
            var fieldName = fieldEntry.Name;
            var line = GetLineNumber(token);

            if (TryConvertToFieldType(field.FieldType, token, fieldName, recordIndex, line, issues, unsupportedFieldWarningsIssued, out var value))
            {
                field.SetValue(record, value);
            }
        }

        /// <summary>
        /// レコードの string / List / 配列フィールドを Unity 慣習に合わせて補正します。
        /// </summary>
        public static void NormalizeRecord(object record, SerializedFieldMap map)
        {
            for (var i = 0; i < map.Fields.Count; i++)
            {
                var field = map.Fields[i].FieldInfo;
                var fieldType = field.FieldType;

                if (fieldType == typeof(string))
                {
                    if (field.GetValue(record) == null)
                    {
                        field.SetValue(record, string.Empty);
                    }

                    continue;
                }

                if (fieldType.IsArray)
                {
                    if (field.GetValue(record) == null)
                    {
                        var elementType = fieldType.GetElementType();
                        var emptyArray = Array.CreateInstance(elementType, 0);
                        field.SetValue(record, emptyArray);
                    }

                    continue;
                }

                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    if (field.GetValue(record) == null)
                    {
                        var list = Activator.CreateInstance(fieldType);
                        field.SetValue(record, list);
                    }
                }
            }
        }

        static bool TryConvertToFieldType(
            Type fieldType,
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued,
            out object value)
        {
            value = GetDefaultValue(fieldType);

            if (fieldType == typeof(int))
            {
                return TryConvertInt(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType == typeof(long))
            {
                return TryConvertLong(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType == typeof(float))
            {
                return TryConvertFloat(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType == typeof(double))
            {
                return TryConvertDouble(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType == typeof(bool))
            {
                return TryConvertBool(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType == typeof(string))
            {
                return TryConvertString(token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType.IsEnum)
            {
                return TryConvertEnum(fieldType, token, fieldName, recordIndex, line, issues, out value);
            }

            if (fieldType.IsArray)
            {
                return TryConvertArray(fieldType, token, fieldName, recordIndex, line, issues, unsupportedFieldWarningsIssued, out value);
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return TryConvertList(fieldType, token, fieldName, recordIndex, line, issues, unsupportedFieldWarningsIssued, out value);
            }

            IssueUnsupportedFieldType(fieldName, recordIndex, line, issues, unsupportedFieldWarningsIssued);
            return false;
        }

        static bool TryConvertInt(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = 0;
            if (!TryParseIntegerToken(token, fieldName, recordIndex, line, issues, out var longValue))
            {
                return false;
            }

            if (longValue < int.MinValue || longValue > int.MaxValue)
            {
                AddError(issues, ImportIssueKind.ValueOutOfRange, FormatRecordFieldMessage(recordIndex, fieldName, "値が int の範囲外です。"), recordIndex, fieldName, line);
                return false;
            }

            value = (int)longValue;
            return true;
        }

        static bool TryConvertLong(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = 0L;
            if (!TryParseIntegerToken(token, fieldName, recordIndex, line, issues, out var longValue))
            {
                return false;
            }

            value = longValue;
            return true;
        }

        static bool TryParseIntegerToken(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out long longValue)
        {
            longValue = 0;

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "整数フィールドに null は指定できません。"), recordIndex, fieldName, line);
                return false;
            }

            if (token.Type != JTokenType.Integer)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "整数フィールドには JSON の整数のみ指定できます。"), recordIndex, fieldName, line);
                return false;
            }

            var jValue = (JValue)token;
            var raw = jValue.Value;
            if (raw is long longRaw)
            {
                longValue = longRaw;
                return true;
            }

            if (raw is int intRaw)
            {
                longValue = intRaw;
                return true;
            }

            if (raw is short shortRaw)
            {
                longValue = shortRaw;
                return true;
            }

            if (raw is byte byteRaw)
            {
                longValue = byteRaw;
                return true;
            }

            if (raw is ulong ulongRaw)
            {
                if (ulongRaw > (ulong)long.MaxValue)
                {
                    AddError(issues, ImportIssueKind.ValueOutOfRange, FormatRecordFieldMessage(recordIndex, fieldName, "整数の範囲外です。"), recordIndex, fieldName, line);
                    return false;
                }

                longValue = (long)ulongRaw;
                return true;
            }

            var text = Convert.ToString(raw, CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(text) ||
                !long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
            {
                AddError(issues, ImportIssueKind.ValueOutOfRange, FormatRecordFieldMessage(recordIndex, fieldName, "整数の範囲外です。"), recordIndex, fieldName, line);
                return false;
            }

            return true;
        }

        static bool TryConvertFloat(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = 0f;
            if (!TryGetNumericDouble(token, fieldName, recordIndex, line, issues, out var number))
            {
                return false;
            }

            var floatValue = (float)number;
            if (float.IsInfinity(floatValue) || float.IsNaN(floatValue))
            {
                AddError(issues, ImportIssueKind.ValueOutOfRange, FormatRecordFieldMessage(recordIndex, fieldName, "float の範囲外です。"), recordIndex, fieldName, line);
                return false;
            }

            value = floatValue;
            return true;
        }

        static bool TryConvertDouble(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = 0d;
            if (!TryGetNumericDouble(token, fieldName, recordIndex, line, issues, out var number))
            {
                return false;
            }

            value = number;
            return true;
        }

        static bool TryGetNumericDouble(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out double number)
        {
            number = 0d;

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "数値フィールドに null は指定できません。"), recordIndex, fieldName, line);
                return false;
            }

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                number = token.Value<double>();
                return true;
            }

            AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "数値フィールドには JSON の整数または小数のみ指定できます。"), recordIndex, fieldName, line);
            return false;
        }

        static bool TryConvertBool(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = false;

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "真偽値フィールドに null は指定できません。"), recordIndex, fieldName, line);
                return false;
            }

            if (token.Type != JTokenType.Boolean)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "真偽値フィールドには JSON の true/false のみ指定できます。"), recordIndex, fieldName, line);
                return false;
            }

            value = token.Value<bool>();
            return true;
        }

        static bool TryConvertString(
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                value = string.Empty;
                return true;
            }

            if (token.Type != JTokenType.String)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "文字列フィールドには JSON の文字列のみ指定できます。"), recordIndex, fieldName, line);
                value = string.Empty;
                return false;
            }

            value = token.Value<string>() ?? string.Empty;
            return true;
        }

        static bool TryConvertEnum(
            Type enumType,
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            out object value)
        {
            value = Activator.CreateInstance(enumType);

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "列挙型フィールドに null は指定できません。"), recordIndex, fieldName, line);
                return false;
            }

            if (token.Type == JTokenType.String)
            {
                var name = token.Value<string>();
                if (string.IsNullOrEmpty(name) || !Enum.IsDefined(enumType, name))
                {
                    AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "列挙型に存在しない定数名です。"), recordIndex, fieldName, line);
                    return false;
                }

                value = Enum.Parse(enumType, name);
                return true;
            }

            if (token.Type == JTokenType.Integer)
            {
                if (!TryParseIntegerToken(token, fieldName, recordIndex, line, issues, out var longValue))
                {
                    return false;
                }

                var underlyingType = Enum.GetUnderlyingType(enumType);
                object numericObject;
                try
                {
                    numericObject = Convert.ChangeType(longValue, underlyingType, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "列挙型に定義されていない整数値です。"), recordIndex, fieldName, line);
                    return false;
                }

                if (IsFlagsEnum(enumType))
                {
                    value = Enum.ToObject(enumType, numericObject);
                    return true;
                }

                if (!Enum.IsDefined(enumType, numericObject))
                {
                    AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "列挙型に定義されていない整数値です。"), recordIndex, fieldName, line);
                    return false;
                }

                value = Enum.ToObject(enumType, numericObject);
                return true;
            }

            AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "列挙型には JSON の文字列または整数のみ指定できます。"), recordIndex, fieldName, line);
            return false;
        }

        static bool TryConvertList(
            Type listType,
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued,
            out object value)
        {
            var elementType = listType.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(listType);
            value = list;

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "配列フィールドに null は指定できません。"), recordIndex, fieldName, line);
                return false;
            }

            if (token.Type != JTokenType.Array)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "List フィールドには JSON の配列のみ指定できます。"), recordIndex, fieldName, line);
                return false;
            }

            var array = (JArray)token;
            for (var i = 0; i < array.Count; i++)
            {
                var elementFieldName = fieldName + "[" + i + "]";
                var elementToken = array[i];
                var elementLine = GetLineNumber(elementToken);

                if (TryConvertToFieldType(elementType, elementToken, elementFieldName, recordIndex, elementLine, issues, unsupportedFieldWarningsIssued, out var elementValue))
                {
                    list.Add(elementValue);
                }
                else
                {
                    list.Add(GetDefaultValue(elementType));
                }
            }

            return true;
        }

        static bool TryConvertArray(
            Type arrayType,
            JToken token,
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued,
            out object value)
        {
            var elementType = arrayType.GetElementType();

            if (token == null || token.Type == JTokenType.Null)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "配列フィールドに null は指定できません。"), recordIndex, fieldName, line);
                value = Array.CreateInstance(elementType, 0);
                return false;
            }

            if (token.Type != JTokenType.Array)
            {
                AddError(issues, ImportIssueKind.TypeMismatch, FormatRecordFieldMessage(recordIndex, fieldName, "配列フィールドには JSON の配列のみ指定できます。"), recordIndex, fieldName, line);
                value = Array.CreateInstance(elementType, 0);
                return false;
            }

            var arrayToken = (JArray)token;
            var result = Array.CreateInstance(elementType, arrayToken.Count);
            value = result;

            for (var i = 0; i < arrayToken.Count; i++)
            {
                var elementFieldName = fieldName + "[" + i + "]";
                var elementToken = arrayToken[i];
                var elementLine = GetLineNumber(elementToken);

                if (TryConvertToFieldType(elementType, elementToken, elementFieldName, recordIndex, elementLine, issues, unsupportedFieldWarningsIssued, out var elementValue))
                {
                    result.SetValue(elementValue, i);
                }
                else
                {
                    result.SetValue(GetDefaultValue(elementType), i);
                }
            }

            return true;
        }

        static void IssueUnsupportedFieldType(
            string fieldName,
            int recordIndex,
            int line,
            IList<ImportIssue> issues,
            HashSet<string> unsupportedFieldWarningsIssued)
        {
            if (!unsupportedFieldWarningsIssued.Add(fieldName))
            {
                return;
            }

            AddWarning(
                issues,
                ImportIssueKind.UnsupportedFieldType,
                FormatRecordFieldMessage(recordIndex, fieldName, "サポートされていないフィールド型です。"),
                recordIndex,
                fieldName,
                line);
        }

        static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        static bool IsFlagsEnum(Type enumType)
        {
            return enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
        }

        static int GetLineNumber(JToken token)
        {
            if (token is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return lineInfo.LineNumber;
            }

            return 0;
        }

        internal static string FormatRecordFieldMessage(int recordIndex, string fieldName, string detail)
        {
            if (recordIndex >= 0 && !string.IsNullOrEmpty(fieldName))
            {
                return "records[" + recordIndex + "]." + fieldName + ": " + detail;
            }

            if (recordIndex >= 0)
            {
                return "records[" + recordIndex + "]: " + detail;
            }

            return detail;
        }

        static void AddError(
            IList<ImportIssue> issues,
            ImportIssueKind kind,
            string message,
            int recordIndex,
            string fieldName,
            int lineNumber)
        {
            issues.Add(new ImportIssue(ImportSeverity.Error, kind, message, recordIndex, fieldName, lineNumber));
        }

        static void AddWarning(
            IList<ImportIssue> issues,
            ImportIssueKind kind,
            string message,
            int recordIndex,
            string fieldName,
            int lineNumber)
        {
            issues.Add(new ImportIssue(ImportSeverity.Warning, kind, message, recordIndex, fieldName, lineNumber));
        }
    }
}
