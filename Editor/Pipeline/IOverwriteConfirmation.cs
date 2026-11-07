using UnityEditor;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// 既存アセットを上書きする前の確認です。
    /// </summary>
    internal interface IOverwriteConfirmation
    {
        /// <summary>
        /// 上書きしてよいかを尋ねます。
        /// </summary>
        /// <param name="assetPath">保存先のパスです。</param>
        /// <param name="previousRecordCount">既存のレコード件数です。</param>
        /// <param name="newRecordCount">今回のレコード件数です。</param>
        /// <returns>上書きする場合は true です。</returns>
        bool Confirm(string assetPath, int previousRecordCount, int newRecordCount);
    }

    /// <summary>
    /// <see cref="EditorUtility.DisplayDialog"/> で上書きを確認します。
    /// </summary>
    internal sealed class EditorOverwriteConfirmation : IOverwriteConfirmation
    {
        /// <inheritdoc />
        public bool Confirm(string assetPath, int previousRecordCount, int newRecordCount)
        {
            var message = "既存のアセットを更新します。\n" +
                          assetPath +
                          "\nレコード数: " + previousRecordCount + " → " + newRecordCount +
                          "\n\nよろしいですか?";
            return EditorUtility.DisplayDialog("既存のアセットを更新", message, "更新する", "キャンセル");
        }
    }
}
