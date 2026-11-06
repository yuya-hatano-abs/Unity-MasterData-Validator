namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// コード生成時のマスタ名と名前空間を表します。
    /// </summary>
    public sealed class MasterCodeOptions
    {
        /// <summary>
        /// 生成オプションを初期化します。
        /// </summary>
        /// <param name="masterName">マスタ名です(例: Item)。</param>
        /// <param name="namespace">出力する名前空間です。</param>
        public MasterCodeOptions(string masterName, string @namespace)
        {
            MasterName = masterName;
            Namespace = @namespace;
        }

        /// <summary>マスタ名です。</summary>
        public string MasterName { get; }

        /// <summary>出力する名前空間です。</summary>
        public string Namespace { get; }
    }
}
