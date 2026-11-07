namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// 実行の履歴を 1 行ずつ受け取ります。
    /// </summary>
    internal interface IMasterLogger
    {
        /// <summary>
        /// 1 行を記録します。
        /// </summary>
        /// <param name="level">重大度です。</param>
        /// <param name="message">本文です。接頭辞は付けません。</param>
        void Log(MasterLogLevel level, string message);
    }
}
