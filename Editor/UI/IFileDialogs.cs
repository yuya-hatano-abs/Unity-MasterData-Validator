using UnityEditor;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// ファイル選択ダイアログです。テストでは差し替えます。
    /// </summary>
    internal interface IFileDialogs
    {
        /// <summary>
        /// JSON ファイルを開くダイアログを表示します。
        /// </summary>
        /// <param name="initialDirectory">初期ディレクトリです。</param>
        /// <returns>選択されたパスです。キャンセルは空文字または null です。</returns>
        string OpenJsonFile(string initialDirectory);

        /// <summary>
        /// 保存先アセットのダイアログを表示します。
        /// </summary>
        /// <param name="suggestedFileName">既定のファイル名です。</param>
        /// <param name="initialAssetFolder">初期フォルダ(Assets/...)です。</param>
        /// <returns>プロジェクト相対パスです。キャンセルは空文字または null です。</returns>
        string SaveAssetFile(string suggestedFileName, string initialAssetFolder);
    }

    /// <summary>
    /// <see cref="EditorUtility"/> を使うファイル選択です。
    /// </summary>
    internal sealed class EditorFileDialogs : IFileDialogs
    {
        /// <inheritdoc />
        public string OpenJsonFile(string initialDirectory)
        {
            return EditorUtility.OpenFilePanel("JSON ファイル", initialDirectory ?? string.Empty, "json");
        }

        /// <inheritdoc />
        public string SaveAssetFile(string suggestedFileName, string initialAssetFolder)
        {
            var folder = string.IsNullOrEmpty(initialAssetFolder) ? "Assets" : initialAssetFolder;
            return EditorUtility.SaveFilePanelInProject(
                "保存先アセット",
                suggestedFileName,
                "asset",
                "保存先のアセットを選択してください。",
                folder);
        }
    }
}
