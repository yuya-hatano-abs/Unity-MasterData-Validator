using System;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// バリデーションで検出された 1 件の問題です。
    /// </summary>
    public sealed class ValidationIssue
    {
        /// <summary>
        /// 問題を表すインスタンスを初期化します。
        /// </summary>
        /// <param name="severity">重大度です。</param>
        /// <param name="kind">問題の種類です。</param>
        /// <param name="validatorId">報告した検証器の ID です。</param>
        /// <param name="message">日本語メッセージです。</param>
        /// <param name="masterName">マスタ名です。</param>
        /// <param name="assetPath">アセットパスです。</param>
        /// <param name="recordIndex">レコードインデックスです。</param>
        /// <param name="recordKey">レコードキーです。</param>
        /// <param name="fieldName">フィールド名です。</param>
        /// <param name="lineNumber">JSON 行番号です。</param>
        public ValidationIssue(
            ValidationSeverity severity,
            ValidationIssueKind kind,
            string validatorId,
            string message,
            string masterName = null,
            string assetPath = null,
            int recordIndex = -1,
            string recordKey = null,
            string fieldName = null,
            int lineNumber = 0)
        {
            if (validatorId == null)
            {
                throw new ArgumentNullException(nameof(validatorId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Severity = severity;
            Kind = kind;
            ValidatorId = validatorId;
            Message = message;
            MasterName = masterName;
            AssetPath = assetPath;
            RecordIndex = recordIndex;
            RecordKey = recordKey;
            FieldName = fieldName;
            LineNumber = lineNumber;
        }

        /// <summary>重大度です。</summary>
        public ValidationSeverity Severity { get; }

        /// <summary>問題の種類です。</summary>
        public ValidationIssueKind Kind { get; }

        /// <summary>報告した検証器の ID です。</summary>
        public string ValidatorId { get; }

        /// <summary>日本語メッセージです。</summary>
        public string Message { get; }

        /// <summary>マスタ名です。</summary>
        public string MasterName { get; }

        /// <summary>アセットパスです。</summary>
        public string AssetPath { get; }

        /// <summary>
        /// レコードのインデックスです。該当なしは -1 です。
        /// 意味は問題の由来によって異なります。
        /// インポート由来(<see cref="ValidatorId"/> が <see cref="ImportIssueConverter.ValidatorId"/>)の場合は、
        /// ソース JSON の records 配列上の位置(スキップされた要素を含む)です。
        /// 検証器由来の場合は、<see cref="MasterTable.Records"/> 上の 0 始まりのインデックスです。
        /// 検証器は、可能な場合は <see cref="RecordKey"/> も併せて設定することを推奨します。
        /// </summary>
        public int RecordIndex { get; }

        /// <summary>レコードの Key です。該当なしは null です。</summary>
        public string RecordKey { get; }

        /// <summary>フィールド名です。該当なしは null です。</summary>
        public string FieldName { get; }

        /// <summary>JSON 上の行番号です。不明な場合は 0 です。</summary>
        public int LineNumber { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return "[" + Severity + "] " + Kind + " (" + ValidatorId + "): " + Message;
        }
    }
}
