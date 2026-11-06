using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// 各マスタテーブル内で <see cref="IMasterRecord.Key"/> の重複を検出する検証器です。
    /// </summary>
    /// <remarks>
    /// 最初に出現したレコードは報告しません。2 件目以降のレコードごとに 1 件ずつ報告します。
    /// これは実行時の検索(<see cref="MasterDataBase{TRecord}.TryGetByKey"/>)が、
    /// 先に出てきたレコードを返す仕様と対応しています。
    /// null のレコード、および Key が null または空のレコードは読み飛ばします(Task 9 で必須項目として扱います)。
    /// </remarks>
    public sealed class DuplicateIdValidator : IValidator
    {
        /// <summary>この検証器の識別子です。</summary>
        public const string ValidatorId = "duplicate-id";

        /// <inheritdoc />
        public string Id => ValidatorId;

        /// <inheritdoc />
        public ValidationResult Validate(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var issues = new List<ValidationIssue>();
            var targets = context.Targets;
            for (var t = 0; t < targets.Count; t++)
            {
                CollectIssuesForTable(targets[t], issues);
            }

            if (issues.Count == 0)
            {
                return ValidationResult.Empty;
            }

            return new ValidationResult(issues);
        }

        static void CollectIssuesForTable(MasterTable table, List<ValidationIssue> issues)
        {
            var firstIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
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

                if (!firstIndexByKey.TryGetValue(key, out var firstIndex))
                {
                    firstIndexByKey[key] = i;
                    continue;
                }

                var message = "マスタ '" + table.Name + "' の records[" + i + "] の ID '" + key +
                              "' は、records[" + firstIndex + "] と重複しています。";

                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    ValidationIssueKind.DuplicateId,
                    ValidatorId,
                    message,
                    table.Name,
                    table.AssetPath,
                    i,
                    key,
                    "id",
                    0));
            }
        }
    }
}
