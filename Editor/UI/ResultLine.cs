namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 実行結果の 1 行の重大度です。
    /// </summary>
    internal enum ResultLineSeverity
    {
        /// <summary>案内です。</summary>
        Info,

        /// <summary>警告です。</summary>
        Warning,

        /// <summary>エラーです。</summary>
        Error,
    }

    /// <summary>
    /// 実行結果の表示用の 1 行です。
    /// </summary>
    internal sealed class ResultLine
    {
        /// <summary>
        /// 結果の行を初期化します。
        /// </summary>
        /// <param name="severity">重大度です。</param>
        /// <param name="text">表示する文です。</param>
        public ResultLine(ResultLineSeverity severity, string text)
        {
            Severity = severity;
            Text = text ?? string.Empty;
        }

        /// <summary>重大度です。</summary>
        public ResultLineSeverity Severity { get; }

        /// <summary>表示する文です。</summary>
        public string Text { get; }
    }
}
