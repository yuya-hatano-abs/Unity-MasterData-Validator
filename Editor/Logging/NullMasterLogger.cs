namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// 何も出力しないロガーです。
    /// </summary>
    internal sealed class NullMasterLogger : IMasterLogger
    {
        /// <summary>共有インスタンスです。</summary>
        public static readonly NullMasterLogger Instance = new NullMasterLogger();

        NullMasterLogger()
        {
        }

        /// <inheritdoc />
        public void Log(MasterLogLevel level, string message)
        {
        }
    }
}
