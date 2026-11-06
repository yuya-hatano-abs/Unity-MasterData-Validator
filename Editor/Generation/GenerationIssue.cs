using MasterDataTool.EditorTools.Importing;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// コード生成・スキーマ推論で発生した問題 1 件を表します。
    /// </summary>
    public sealed class GenerationIssue
    {
        /// <summary>
        /// 問題を表すインスタンスを初期化します。
        /// </summary>
        /// <param name="severity">重大度です。</param>
        /// <param name="kind">問題の種類です。</param>
        /// <param name="message">日本語メッセージです。</param>
        /// <param name="fieldName">関連する JSON キーです。該当がなければ null です。</param>
        /// <param name="lineNumber">行番号です。不明な場合は 0 です。</param>
        public GenerationIssue(
            ImportSeverity severity,
            GenerationIssueKind kind,
            string message,
            string fieldName,
            int lineNumber)
        {
            Severity = severity;
            Kind = kind;
            Message = message;
            FieldName = fieldName;
            LineNumber = lineNumber;
        }

        /// <summary>重大度です。</summary>
        public ImportSeverity Severity { get; }

        /// <summary>問題の種類です。</summary>
        public GenerationIssueKind Kind { get; }

        /// <summary>日本語メッセージです。</summary>
        public string Message { get; }

        /// <summary>関連する JSON キーです。</summary>
        public string FieldName { get; }

        /// <summary>行番号です。</summary>
        public int LineNumber { get; }
    }
}
