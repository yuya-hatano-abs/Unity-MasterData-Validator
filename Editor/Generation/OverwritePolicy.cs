namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// 既存ファイルがある場合の書き込み方針です。
    /// </summary>
    public enum OverwritePolicy
    {
        /// <summary>既存ファイルがある場合は書き込みません。</summary>
        Fail,

        /// <summary>既存ファイルを上書きします。</summary>
        Overwrite,
    }
}
