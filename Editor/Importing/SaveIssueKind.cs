namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// マスタアセット保存時の問題の種類です。いずれも Error 相当です。
    /// </summary>
    public enum SaveIssueKind
    {
        /// <summary>インポート結果の Succeeded が false です。</summary>
        ImportNotSucceeded,

        /// <summary>インポート結果に Error が含まれます。</summary>
        ImportHasErrors,

        /// <summary>マスタ型が abstract など、アセットを作れません。</summary>
        InvalidMasterType,

        /// <summary>アセットパスが不正です。</summary>
        InvalidAssetPath,

        /// <summary>既存アセットがあり、上書きポリシーが Fail です。</summary>
        AssetExists,

        /// <summary>パス上のアセット型が TMaster と互換性がありません。</summary>
        AssetTypeMismatch,

        /// <summary>親フォルダが存在せず、CreateFolders が false です。</summary>
        FolderNotFound,

        /// <summary>AssetDatabase.CreateFolder に失敗しました。</summary>
        FolderCreationFailed,

        /// <summary>AssetDatabase の保存操作に失敗しました。</summary>
        SaveFailed,
    }
}
