using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Importing;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// Task 3 の ImportIssue を ValidationIssue に変換します。
    /// </summary>
    public static class ImportIssueConverter
    {
        /// <summary>インポート由来の問題に付与する検証器 ID です。</summary>
        public const string ValidatorId = "import";

        /// <summary>
        /// ImportIssue を ValidationIssue に変換します。
        /// </summary>
        /// <param name="issue">インポート問題です。</param>
        /// <param name="masterName">マスタ名です。</param>
        /// <param name="assetPath">アセットパスです。</param>
        /// <returns>バリデーション問題です。</returns>
        public static ValidationIssue Convert(ImportIssue issue, string masterName, string assetPath)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            return new ValidationIssue(
                ConvertSeverity(issue.Severity),
                ConvertKind(issue.Kind),
                ValidatorId,
                issue.Message,
                masterName,
                assetPath,
                issue.RecordIndex,
                recordKey: null,
                issue.FieldName,
                issue.LineNumber);
        }

        /// <summary>
        /// ImportIssue の列を ValidationIssue の列に変換します。
        /// </summary>
        /// <param name="issues">インポート問題の列です。</param>
        /// <param name="masterName">マスタ名です。</param>
        /// <param name="assetPath">アセットパスです。</param>
        /// <returns>バリデーション問題のリストです。</returns>
        public static IReadOnlyList<ValidationIssue> ConvertAll(
            IEnumerable<ImportIssue> issues,
            string masterName,
            string assetPath)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var list = new List<ValidationIssue>();
            foreach (var issue in issues)
            {
                list.Add(Convert(issue, masterName, assetPath));
            }

            return list;
        }

        static ValidationSeverity ConvertSeverity(ImportSeverity severity)
        {
            return severity == ImportSeverity.Warning
                ? ValidationSeverity.Warning
                : ValidationSeverity.Error;
        }

        /// <summary>
        /// ImportIssueKind を ValidationIssueKind に変換します。
        /// 将来 ImportIssueKind に未知の値が追加された場合は ImportFailed にフォールバックします(例外にしません)。
        /// </summary>
        static ValidationIssueKind ConvertKind(ImportIssueKind kind)
        {
            switch (kind)
            {
                case ImportIssueKind.InvalidJson:
                case ImportIssueKind.FileNotFound:
                case ImportIssueKind.MissingRecordsArray:
                case ImportIssueKind.RecordNotObject:
                case ImportIssueKind.UnsupportedRecordType:
                case ImportIssueKind.DuplicateFieldName:
                    return ValidationIssueKind.ImportFailed;
                case ImportIssueKind.TypeMismatch:
                    return ValidationIssueKind.TypeMismatch;
                case ImportIssueKind.ValueOutOfRange:
                    return ValidationIssueKind.ValueOutOfRange;
                case ImportIssueKind.UnknownField:
                    return ValidationIssueKind.UnknownField;
                case ImportIssueKind.MissingField:
                    return ValidationIssueKind.MissingField;
                case ImportIssueKind.UnsupportedFieldType:
                    return ValidationIssueKind.UnsupportedFieldType;
                default:
                    return ValidationIssueKind.ImportFailed;
            }
        }
    }
}
