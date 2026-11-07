using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 検証問題の 1 行を、一覧用の文字列にします。
    /// </summary>
    internal static class IssueRowFormatter
    {
        /// <summary>
        /// 上段の本文です。重大度とメッセージです。
        /// </summary>
        /// <param name="issue">問題です。</param>
        /// <returns>上段の文字列です。</returns>
        public static string FormatPrimary(ValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            var prefix = issue.Severity == ValidationSeverity.Error ? "[エラー] " : "[警告] ";
            return prefix + issue.Message;
        }

        /// <summary>
        /// 下段の補足です。ある項目だけを「 ・ 」でつなぎます。
        /// </summary>
        /// <param name="issue">問題です。</param>
        /// <returns>下段の文字列です。</returns>
        public static string FormatDetail(ValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            var parts = new List<string>();
            parts.Add(ValidationKindLabels.GetLabel(issue.Kind));
            if (!string.IsNullOrEmpty(issue.MasterName))
            {
                parts.Add(issue.MasterName);
            }

            if (issue.RecordIndex >= 0)
            {
                parts.Add("records[" + issue.RecordIndex + "]");
            }

            if (!string.IsNullOrEmpty(issue.RecordKey))
            {
                parts.Add("ID: " + issue.RecordKey);
            }

            if (!string.IsNullOrEmpty(issue.FieldName))
            {
                parts.Add("フィールド: " + issue.FieldName);
            }

            if (issue.LineNumber >= 1)
            {
                parts.Add("JSON " + issue.LineNumber + " 行目");
            }

            return string.Join(" ・ ", parts);
        }

        /// <summary>
        /// ツールチップです。メッセージ、アセットパス、検証器 ID です。
        /// </summary>
        /// <param name="issue">問題です。</param>
        /// <returns>ツールチップの文字列です。</returns>
        public static string FormatTooltip(ValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            var text = issue.Message;
            if (!string.IsNullOrEmpty(issue.AssetPath))
            {
                text += "\nアセット: " + issue.AssetPath;
            }

            text += "\n検証器: " + issue.ValidatorId;
            return text;
        }
    }
}
