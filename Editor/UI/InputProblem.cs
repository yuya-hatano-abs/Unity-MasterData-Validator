namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 入力検査の対象欄です。
    /// </summary>
    internal enum InputField
    {
        /// <summary>対象のマスタ型です。</summary>
        MasterType,

        /// <summary>JSON ファイルのパスです。</summary>
        JsonPath,

        /// <summary>保存先アセットのパスです。</summary>
        AssetPath,
    }

    /// <summary>
    /// 入力検査の重大度です。
    /// </summary>
    internal enum InputProblemSeverity
    {
        /// <summary>進行を妨げない案内です。</summary>
        Info,

        /// <summary>進行を妨げる問題です。</summary>
        Error,
    }

    /// <summary>
    /// 入力検査で見つかった 1 件の問題です。
    /// </summary>
    internal sealed class InputProblem
    {
        /// <summary>
        /// 入力の問題を初期化します。
        /// </summary>
        /// <param name="field">対象欄です。</param>
        /// <param name="severity">重大度です。</param>
        /// <param name="message">日本語メッセージです。</param>
        public InputProblem(InputField field, InputProblemSeverity severity, string message)
        {
            Field = field;
            Severity = severity;
            Message = message;
        }

        /// <summary>対象欄です。</summary>
        public InputField Field { get; }

        /// <summary>重大度です。</summary>
        public InputProblemSeverity Severity { get; }

        /// <summary>日本語メッセージです。</summary>
        public string Message { get; }
    }
}
