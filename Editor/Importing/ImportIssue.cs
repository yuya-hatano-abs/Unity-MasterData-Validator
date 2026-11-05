namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// インポート処理中に検出された 1 件の問題です。
    /// </summary>
    public sealed class ImportIssue
    {
        /// <summary>問題を生成します。</summary>
        public ImportIssue(
            ImportSeverity severity,
            ImportIssueKind kind,
            string message,
            int recordIndex,
            string fieldName,
            int lineNumber)
        {
            Severity = severity;
            Kind = kind;
            Message = message;
            RecordIndex = recordIndex;
            FieldName = fieldName;
            LineNumber = lineNumber;
        }

        /// <summary>重大度です。</summary>
        public ImportSeverity Severity { get; }

        /// <summary>種類です。</summary>
        public ImportIssueKind Kind { get; }

        /// <summary>日本語メッセージです。</summary>
        public string Message { get; }

        /// <summary>records 配列内のインデックス(0 始まり)。該当なしは -1。</summary>
        public int RecordIndex { get; }

        /// <summary>フィールド名。配列要素は tags[2] 形式。該当なしは null。</summary>
        public string FieldName { get; }

        /// <summary>JSON 上の行番号。不明な場合は 0。</summary>
        public int LineNumber { get; }
    }
}
