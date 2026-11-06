using MasterDataTool.EditorTools.Generation;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// マスタアセット保存時のオプションです。
    /// </summary>
    public sealed class MasterAssetSaveOptions
    {
        /// <summary>
        /// 既定のオプションを初期化します。
        /// </summary>
        public MasterAssetSaveOptions()
        {
            ExistingAsset = OverwritePolicy.Fail;
            CreateFolders = true;
        }

        /// <summary>既存アセットがある場合の方針です。既定は Fail です。</summary>
        public OverwritePolicy ExistingAsset { get; set; }

        /// <summary>親フォルダが無い場合に AssetDatabase で作成するかどうかです。既定は true です。</summary>
        public bool CreateFolders { get; set; }
    }
}
