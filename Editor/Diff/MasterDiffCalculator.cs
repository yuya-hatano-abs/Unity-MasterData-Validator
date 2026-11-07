using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Diff
{
    /// <summary>
    /// 既存レコードと新しいレコードを、Key で突き合わせて差分にします。UI には依存しません。
    /// </summary>
    internal static class MasterDiffCalculator
    {
        /// <summary>
        /// 差分を計算します。空でない Key の重複、空の Key、null レコードがあるときは件数を出しません。
        /// </summary>
        /// <param name="existingRecords">既存のレコードです。null は空として扱います。</param>
        /// <param name="incomingRecords">新しいレコードです。null は空として扱います。</param>
        /// <param name="recordType">フィールド一覧に使うレコード型です。</param>
        /// <param name="serializedRecordsEqual">保存する records がシリアライズとして同一なら true です。</param>
        /// <returns>差分です。</returns>
        public static MasterDiffReport Calculate(
            IReadOnlyList<IMasterRecord> existingRecords,
            IReadOnlyList<IMasterRecord> incomingRecords,
            Type recordType,
            bool serializedRecordsEqual)
        {
            if (recordType == null)
            {
                return MasterDiffReport.Unavailable("レコード型がありません。", serializedRecordsEqual);
            }

            var existing = existingRecords ?? new IMasterRecord[0];
            var incoming = incomingRecords ?? new IMasterRecord[0];
            var existingProblem = CollectProblems(existing);
            var incomingProblem = CollectProblems(incoming);
            if (existingProblem.HasProblem || incomingProblem.HasProblem)
            {
                return MasterDiffReport.Unavailable(
                    FormatProblems(existingProblem, incomingProblem),
                    serializedRecordsEqual);
            }

            var existingByKey = Index(existing);
            var incomingByKey = Index(incoming);
            var added = new List<string>();
            var removed = new List<string>();
            var changes = new List<MasterRecordChange>();
            var unchanged = 0;

            for (var i = 0; i < existing.Count; i++)
            {
                var key = existing[i].Key;
                if (!incomingByKey.ContainsKey(key))
                {
                    removed.Add(key);
                }
            }

            var fieldMap = SerializedFieldMap.Get(recordType);
            for (var i = 0; i < incoming.Count; i++)
            {
                var record = incoming[i];
                var key = record.Key;
                if (!existingByKey.TryGetValue(key, out var previous))
                {
                    added.Add(key);
                    continue;
                }

                var fields = CompareFields(fieldMap, previous, record);
                if (fields.Count == 0)
                {
                    unchanged++;
                }
                else
                {
                    changes.Add(new MasterRecordChange(key, fields));
                }
            }

            var orderChanged = !SameOrder(CommonKeys(existing, incomingByKey), CommonKeys(incoming, existingByKey));
            return MasterDiffReport.Available(added, removed, changes, unchanged, orderChanged, serializedRecordsEqual);
        }

        static SideProblem CollectProblems(IReadOnlyList<IMasterRecord> records)
        {
            var nullCount = 0;
            var emptyKeyCount = 0;
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var order = new List<string>();
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record == null)
                {
                    nullCount++;
                    continue;
                }

                var key = record.Key;
                if (string.IsNullOrEmpty(key))
                {
                    emptyKeyCount++;
                    continue;
                }

                if (counts.TryGetValue(key, out var count))
                {
                    counts[key] = count + 1;
                }
                else
                {
                    counts[key] = 1;
                    order.Add(key);
                }
            }

            var duplicates = new List<string>();
            for (var i = 0; i < order.Count; i++)
            {
                var key = order[i];
                if (counts[key] > 1)
                {
                    duplicates.Add(key);
                }
            }

            return new SideProblem(nullCount, emptyKeyCount, duplicates);
        }

        static string FormatProblems(SideProblem existing, SideProblem incoming)
        {
            var parts = new List<string>();
            AppendSide(parts, "既存", existing);
            AppendSide(parts, "新規", incoming);
            var text = string.Empty;
            for (var i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    text += "。";
                }

                text += parts[i];
            }

            return text;
        }

        static void AppendSide(List<string> parts, string side, SideProblem problem)
        {
            if (!problem.HasProblem)
            {
                return;
            }

            var details = new List<string>();
            if (problem.DuplicateKeys.Count > 0)
            {
                var keys = string.Empty;
                for (var i = 0; i < problem.DuplicateKeys.Count; i++)
                {
                    if (i > 0)
                    {
                        keys += ", ";
                    }

                    keys += problem.DuplicateKeys[i];
                }

                details.Add("重複キー " + keys);
            }

            if (problem.EmptyKeyCount > 0)
            {
                details.Add("空のキー " + problem.EmptyKeyCount + " 件");
            }

            if (problem.NullCount > 0)
            {
                details.Add("null " + problem.NullCount + " 件");
            }

            var body = string.Empty;
            for (var i = 0; i < details.Count; i++)
            {
                if (i > 0)
                {
                    body += " / ";
                }

                body += details[i];
            }

            parts.Add(side + ": " + body);
        }

        static Dictionary<string, IMasterRecord> Index(IReadOnlyList<IMasterRecord> records)
        {
            var map = new Dictionary<string, IMasterRecord>(StringComparer.Ordinal);
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                map[record.Key] = record;
            }

            return map;
        }

        static List<string> CommonKeys(
            IReadOnlyList<IMasterRecord> records,
            Dictionary<string, IMasterRecord> other)
        {
            var keys = new List<string>();
            for (var i = 0; i < records.Count; i++)
            {
                var key = records[i].Key;
                if (other.ContainsKey(key))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        static bool SameOrder(List<string> left, List<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        static List<MasterFieldChange> CompareFields(SerializedFieldMap map, IMasterRecord previous, IMasterRecord incoming)
        {
            var changes = new List<MasterFieldChange>();
            var fields = map.Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                var entry = fields[i];
                var field = entry.FieldInfo;
                var oldValue = field.GetValue(previous);
                var newValue = field.GetValue(incoming);
                if (ValuesEqual(oldValue, newValue, field.FieldType))
                {
                    continue;
                }

                changes.Add(new MasterFieldChange(
                    entry.Name,
                    Describe(oldValue, field.FieldType),
                    Describe(newValue, field.FieldType)));
            }

            return changes;
        }

        static bool ValuesEqual(object left, object right, Type type)
        {
            if (type == typeof(float))
            {
                return FloatEqual(left, right);
            }

            if (type == typeof(double))
            {
                return DoubleEqual(left, right);
            }

            if (type == typeof(string))
            {
                if (left == null && right == null)
                {
                    return true;
                }

                if (left == null || right == null)
                {
                    return false;
                }

                return string.Equals((string)left, (string)right, StringComparison.Ordinal);
            }

            if (type.IsArray || IsList(type))
            {
                if (left == null && right == null)
                {
                    return true;
                }

                if (left == null || right == null)
                {
                    return false;
                }

                var leftList = left as IList;
                var rightList = right as IList;
                if (leftList == null || rightList == null || leftList.Count != rightList.Count)
                {
                    return false;
                }

                var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                for (var i = 0; i < leftList.Count; i++)
                {
                    if (!ValuesEqual(leftList[i], rightList[i], elementType))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (IsNestedSerializable(type))
            {
                if (left == null && right == null)
                {
                    return true;
                }

                if (left == null || right == null)
                {
                    return false;
                }

                return string.Equals(SafeJson(left), SafeJson(right), StringComparison.Ordinal);
            }

            return Equals(left, right);
        }

        static bool FloatEqual(object left, object right)
        {
            var leftValue = left == null ? 0f : (float)left;
            var rightValue = right == null ? 0f : (float)right;
            if (float.IsNaN(leftValue) && float.IsNaN(rightValue))
            {
                return true;
            }

            return leftValue == rightValue;
        }

        static bool DoubleEqual(object left, object right)
        {
            var leftValue = left == null ? 0d : (double)left;
            var rightValue = right == null ? 0d : (double)right;
            if (double.IsNaN(leftValue) && double.IsNaN(rightValue))
            {
                return true;
            }

            return leftValue == rightValue;
        }

        static MasterDiffValue Describe(object value, Type type)
        {
            if (type == typeof(string))
            {
                if (value == null)
                {
                    return MasterDiffValue.Scalar("(null)");
                }

                var text = (string)value;
                return MasterDiffValue.Scalar(text.Length == 0 ? "(空)" : text);
            }

            if (type.IsArray || IsList(type))
            {
                if (value == null)
                {
                    return MasterDiffValue.Scalar("(null)");
                }

                var list = value as IList;
                if (list == null)
                {
                    return MasterDiffValue.Scalar(value.ToString());
                }

                var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                var elements = new List<string>();
                for (var i = 0; i < list.Count; i++)
                {
                    elements.Add(Describe(list[i], elementType).Text);
                }

                return MasterDiffValue.Collection(elements);
            }

            if (value == null)
            {
                return MasterDiffValue.Scalar("(null)");
            }

            if (type == typeof(float))
            {
                var number = (float)value;
                return MasterDiffValue.Scalar(
                    float.IsNaN(number) ? "NaN" : number.ToString("R", CultureInfo.InvariantCulture));
            }

            if (type == typeof(double))
            {
                var number = (double)value;
                return MasterDiffValue.Scalar(
                    double.IsNaN(number) ? "NaN" : number.ToString("R", CultureInfo.InvariantCulture));
            }

            if (IsNestedSerializable(type))
            {
                return MasterDiffValue.Scalar(SafeJson(value));
            }

            if (type == typeof(bool) || type.IsPrimitive || type.IsEnum)
            {
                return MasterDiffValue.Scalar(Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            return MasterDiffValue.Scalar(value.ToString());
        }

        static string SafeJson(object value)
        {
            try
            {
                return JsonUtility.ToJson(value);
            }
            catch (Exception)
            {
                return value == null ? string.Empty : value.ToString();
            }
        }

        static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        static bool IsNestedSerializable(Type type)
        {
            if (type == null || type == typeof(string) || type.IsPrimitive || type.IsEnum || type.IsArray || IsList(type))
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            return Attribute.IsDefined(type, typeof(SerializableAttribute), false);
        }

        sealed class SideProblem
        {
            public SideProblem(int nullCount, int emptyKeyCount, List<string> duplicateKeys)
            {
                NullCount = nullCount;
                EmptyKeyCount = emptyKeyCount;
                DuplicateKeys = duplicateKeys;
            }

            public int NullCount { get; }

            public int EmptyKeyCount { get; }

            public List<string> DuplicateKeys { get; }

            public bool HasProblem
            {
                get { return NullCount > 0 || EmptyKeyCount > 0 || DuplicateKeys.Count > 0; }
            }
        }
    }
}
