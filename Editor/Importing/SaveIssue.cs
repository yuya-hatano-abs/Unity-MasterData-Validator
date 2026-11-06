namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// マスタアセット保存時の問題 1 件を表します。
    /// </summary>
    public sealed class SaveIssue
    {
        /// <summary>
        /// 問題を表すインスタンスを初期化します。
        /// </summary>
        /// <param name="kind">問題の種類です。</param>
        /// <param name="message">日本語メッセージです。</param>
        public SaveIssue(SaveIssueKind kind, string message)
        {
            Kind = kind;
            Message = message;
        }

        /// <summary>問題の種類です。</summary>
        public SaveIssueKind Kind { get; }

        /// <summary>日本語メッセージです。</summary>
        public string Message { get; }
    }
}
