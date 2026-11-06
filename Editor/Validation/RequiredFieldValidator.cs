using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// [MasterRequired] 付きフィールドと、文字列 ID、null レコードの必須を検査する検証器です。
    /// </summary>
    /// <remarks>
    /// 検証対象は <see cref="ValidationContext.Targets"/> です。<see cref="ValidationContext.Tables"/> は使いません。
    /// 検証器はレコードを変更しません。
    /// </remarks>
    public sealed class RequiredFieldValidator : IValidator
    {
        /// <summary>この検証器の識別子です。</summary>
        public const string ValidatorId = "required-field";

        /// <inheritdoc />
        public string Id => ValidatorId;

        /// <inheritdoc />
        public ValidationResult Validate(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var fieldCache = new Dictionary<Type, IReadOnlyList<CollectedRequiredField>>();
            var issues = new List<ValidationIssue>();
            var targets = context.Targets;

            for (var t = 0; t < targets.Count; t++)
            {
                CollectIssuesForTable(targets[t], fieldCache, issues);
            }

            if (issues.Count == 0)
            {
                return ValidationResult.Empty;
            }

            return new ValidationResult(issues);
        }

        static void CollectIssuesForTable(
            MasterTable table,
            Dictionary<Type, IReadOnlyList<CollectedRequiredField>> fieldCache,
            List<ValidationIssue> issues)
        {
            var collected = GetCollectedFields(fieldCache, table.RecordType);
            var inspectable = new List<CollectedRequiredField>();

            for (var i = 0; i < collected.Count; i++)
            {
                var field = collected[i];
                if (field.DefinitionError != null)
                {
                    issues.Add(CreateIssue(
                        table,
                        ValidationIssueKind.InvalidRequiredDefinition,
                        "マスタ '" + table.Name + "' のフィールド '" + field.Field.Name + "' " +
                        field.DefinitionError,
                        -1,
                        null,
                        field.Field.Name));
                    continue;
                }

                inspectable.Add(field);
            }

            var records = table.Records;
            for (var r = 0; r < records.Count; r++)
            {
                var record = records[r];
                if (record == null)
                {
                    issues.Add(CreateIssue(
                        table,
                        ValidationIssueKind.MissingRequiredField,
                        "マスタ '" + table.Name + "' の records[" + r + "] が null です。",
                        r,
                        null,
                        null));
                    continue;
                }

                if (table.IdKind == MasterIdKind.String && string.IsNullOrWhiteSpace(record.Key))
                {
                    issues.Add(CreateIssue(
                        table,
                        ValidationIssueKind.MissingRequiredField,
                        "マスタ '" + table.Name + "' の records[" + r +
                        "] の ID が空です(文字列 ID は必須です)。",
                        r,
                        record.Key,
                        "id"));
                }

                for (var f = 0; f < inspectable.Count; f++)
                {
                    var field = inspectable[f];
                    if (!IsEmpty(field.Field, record))
                    {
                        continue;
                    }

                    issues.Add(CreateIssue(
                        table,
                        ValidationIssueKind.MissingRequiredField,
                        "マスタ '" + table.Name + "' の records[" + r + "]." + field.Field.Name +
                        " は必須ですが、空です。",
                        r,
                        record.Key,
                        field.Field.Name));
                }
            }
        }

        static IReadOnlyList<CollectedRequiredField> GetCollectedFields(
            Dictionary<Type, IReadOnlyList<CollectedRequiredField>> cache,
            Type recordType)
        {
            if (!cache.TryGetValue(recordType, out var collected))
            {
                collected = RequiredFieldCollector.Collect(recordType);
                cache[recordType] = collected;
            }

            return collected;
        }

        static bool IsEmpty(FieldInfo field, IMasterRecord record)
        {
            var value = field.GetValue(record);
            if (field.FieldType == typeof(string))
            {
                return string.IsNullOrWhiteSpace(value as string);
            }

            if (value == null)
            {
                return true;
            }

            var collection = value as ICollection;
            if (collection != null)
            {
                return collection.Count == 0;
            }

            var array = value as Array;
            return array != null && array.Length == 0;
        }

        static ValidationIssue CreateIssue(
            MasterTable table,
            ValidationIssueKind kind,
            string message,
            int recordIndex,
            string recordKey,
            string fieldName)
        {
            return new ValidationIssue(
                ValidationSeverity.Error,
                kind,
                ValidatorId,
                message,
                table.Name,
                table.AssetPath,
                recordIndex,
                recordKey,
                fieldName,
                0);
        }
    }
}
