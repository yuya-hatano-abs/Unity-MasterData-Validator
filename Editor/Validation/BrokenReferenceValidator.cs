using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// [MasterRef] 付きフィールドの参照先 ID が、参照先マスタに存在するかを検査する検証器です。
    /// </summary>
    /// <remarks>
    /// 検証対象は <see cref="ValidationContext.Targets"/> のレコードです。
    /// 参照先のテーブルは <see cref="ValidationContext.Tables"/> から引きます(Targets に含まれないテーブルも使えます)。
    /// 検証器はレコードを変更しません。
    /// </remarks>
    public sealed class BrokenReferenceValidator : IValidator
    {
        /// <summary>この検証器の識別子です。</summary>
        public const string ValidatorId = "broken-reference";

        const string NoneValueHint =
            "0 を『参照なし』として扱う場合は、MasterRef に NoneValue = 0 を指定してください。";

        /// <inheritdoc />
        public string Id => ValidatorId;

        /// <inheritdoc />
        public ValidationResult Validate(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var fieldCache = new Dictionary<Type, IReadOnlyList<CollectedReferenceField>>();
            var keyCache = new Dictionary<MasterTable, HashSet<string>>();
            var issues = new List<ValidationIssue>();

            var targets = context.Targets;
            for (var t = 0; t < targets.Count; t++)
            {
                CollectIssuesForTable(targets[t], context, fieldCache, keyCache, issues);
            }

            if (issues.Count == 0)
            {
                return ValidationResult.Empty;
            }

            return new ValidationResult(issues);
        }

        static void CollectIssuesForTable(
            MasterTable table,
            ValidationContext context,
            Dictionary<Type, IReadOnlyList<CollectedReferenceField>> fieldCache,
            Dictionary<MasterTable, HashSet<string>> keyCache,
            List<ValidationIssue> issues)
        {
            var collected = GetCollectedFields(fieldCache, table.RecordType);
            var inspectable = new List<InspectableReference>(collected.Count);

            for (var i = 0; i < collected.Count; i++)
            {
                var field = collected[i];
                if (field.DefinitionError != null)
                {
                    issues.Add(CreateDefinitionIssue(
                        table,
                        field.Field.Name,
                        ValidationIssueKind.InvalidReferenceDefinition,
                        "マスタ '" + table.Name + "' のフィールド '" + field.Field.Name + "' " +
                        field.DefinitionError));
                    continue;
                }

                var targetType = field.Attribute.TargetMasterType;
                if (!context.TryGetTable(targetType, out var targetTable))
                {
                    issues.Add(CreateDefinitionIssue(
                        table,
                        field.Field.Name,
                        ValidationIssueKind.ReferenceTargetUnavailable,
                        "マスタ '" + table.Name + "' のフィールド '" + field.Field.Name +
                        "' の参照先マスタ '" + targetType.Name +
                        "' が、検証の対象に含まれていません。参照先のマスタを、検証の文脈(Tables)に含めてください。"));
                    continue;
                }

                var expectedKind = field.IsIntKind ? MasterIdKind.Int : MasterIdKind.String;
                if (targetTable.IdKind != expectedKind)
                {
                    issues.Add(CreateDefinitionIssue(
                        table,
                        field.Field.Name,
                        ValidationIssueKind.InvalidReferenceDefinition,
                        "マスタ '" + table.Name + "' のフィールド '" + field.Field.Name +
                        "' は " + (field.IsIntKind ? "int" : "string") +
                        " 系ですが、参照先マスタ '" + targetTable.Name +
                        "' の ID 種類は " + targetTable.IdKind + " です。"));
                    continue;
                }

                inspectable.Add(new InspectableReference(field, targetTable, GetOrBuildKeys(keyCache, targetTable)));
            }

            var records = table.Records;
            for (var r = 0; r < records.Count; r++)
            {
                var record = records[r];
                if (record == null)
                {
                    continue;
                }

                for (var f = 0; f < inspectable.Count; f++)
                {
                    InspectRecord(table, record, r, inspectable[f], issues);
                }
            }
        }

        static IReadOnlyList<CollectedReferenceField> GetCollectedFields(
            Dictionary<Type, IReadOnlyList<CollectedReferenceField>> cache,
            Type recordType)
        {
            if (!cache.TryGetValue(recordType, out var collected))
            {
                collected = ReferenceFieldCollector.Collect(recordType);
                cache[recordType] = collected;
            }

            return collected;
        }

        static HashSet<string> GetOrBuildKeys(Dictionary<MasterTable, HashSet<string>> cache, MasterTable table)
        {
            if (!cache.TryGetValue(table, out var keys))
            {
                keys = new HashSet<string>(StringComparer.Ordinal);
                var records = table.Records;
                for (var i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    if (record == null)
                    {
                        continue;
                    }

                    var key = record.Key;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    keys.Add(key);
                }

                cache[table] = keys;
            }

            return keys;
        }

        static void InspectRecord(
            MasterTable table,
            IMasterRecord record,
            int recordIndex,
            InspectableReference inspectable,
            List<ValidationIssue> issues)
        {
            var value = inspectable.Field.Field.GetValue(record);
            if (inspectable.Field.IsCollection)
            {
                if (value == null)
                {
                    return;
                }

                var list = (IList)value;
                for (var i = 0; i < list.Count; i++)
                {
                    TryReportValue(
                        table,
                        record,
                        recordIndex,
                        inspectable,
                        list[i],
                        inspectable.Field.Field.Name + "[" + i + "]",
                        issues);
                }

                return;
            }

            TryReportValue(
                table,
                record,
                recordIndex,
                inspectable,
                value,
                inspectable.Field.Field.Name,
                issues);
        }

        static void TryReportValue(
            MasterTable table,
            IMasterRecord record,
            int recordIndex,
            InspectableReference inspectable,
            object value,
            string fieldName,
            List<ValidationIssue> issues)
        {
            if (inspectable.Field.IsIntKind)
            {
                var intValue = value is int boxed ? boxed : 0;
                if (inspectable.Field.Attribute.HasNoneValue &&
                    intValue == inspectable.Field.Attribute.NoneValue)
                {
                    return;
                }

                var key = intValue.ToString(CultureInfo.InvariantCulture);
                if (inspectable.Keys.Contains(key))
                {
                    return;
                }

                var message = BuildBrokenMessage(table.Name, recordIndex, fieldName, key, inspectable.TargetTable.Name);
                if (intValue == 0 && !inspectable.Field.Attribute.HasNoneValue)
                {
                    message += NoneValueHint;
                }

                issues.Add(CreateBrokenIssue(table, record, recordIndex, fieldName, message));
                return;
            }

            var stringValue = value as string;
            if (string.IsNullOrEmpty(stringValue))
            {
                return;
            }

            if (inspectable.Keys.Contains(stringValue))
            {
                return;
            }

            issues.Add(CreateBrokenIssue(
                table,
                record,
                recordIndex,
                fieldName,
                BuildBrokenMessage(table.Name, recordIndex, fieldName, stringValue, inspectable.TargetTable.Name)));
        }

        static string BuildBrokenMessage(
            string masterName,
            int recordIndex,
            string fieldName,
            string key,
            string targetMasterName)
        {
            return "マスタ '" + masterName + "' の records[" + recordIndex + "]." + fieldName +
                   " の参照先 ID '" + key + "' が、マスタ '" + targetMasterName + "' に存在しません。";
        }

        static ValidationIssue CreateBrokenIssue(
            MasterTable table,
            IMasterRecord record,
            int recordIndex,
            string fieldName,
            string message)
        {
            return new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.BrokenReference,
                ValidatorId,
                message,
                table.Name,
                table.AssetPath,
                recordIndex,
                record.Key,
                fieldName,
                0);
        }

        static ValidationIssue CreateDefinitionIssue(
            MasterTable table,
            string fieldName,
            ValidationIssueKind kind,
            string message)
        {
            return new ValidationIssue(
                ValidationSeverity.Error,
                kind,
                ValidatorId,
                message,
                table.Name,
                table.AssetPath,
                -1,
                null,
                fieldName,
                0);
        }

        sealed class InspectableReference
        {
            public InspectableReference(
                CollectedReferenceField field,
                MasterTable targetTable,
                HashSet<string> keys)
            {
                Field = field;
                TargetTable = targetTable;
                Keys = keys;
            }

            public CollectedReferenceField Field { get; }

            public MasterTable TargetTable { get; }

            public HashSet<string> Keys { get; }
        }
    }
}
