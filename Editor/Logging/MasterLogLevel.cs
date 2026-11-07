namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// 実行ログの重大度です。
    /// </summary>
    internal enum MasterLogLevel
    {
        /// <summary>通常の記録です。</summary>
        Info,

        /// <summary>注意が必要な記録です。</summary>
        Warning,

        /// <summary>失敗の記録です。</summary>
        Error,
    }
}
