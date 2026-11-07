using UnityEditor;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// アセットを Project ウィンドウで選択します。
    /// </summary>
    internal interface IAssetSelector
    {
        /// <summary>
        /// パスのアセットを選択します。
        /// </summary>
        /// <param name="assetPath">Assets パスです。</param>
        /// <returns>選択できた場合は true です。</returns>
        bool TrySelect(string assetPath);
    }

    /// <summary>
    /// <see cref="AssetDatabase"/> と <see cref="Selection"/> でアセットを選択します。
    /// </summary>
    internal sealed class EditorAssetSelector : IAssetSelector
    {
        /// <inheritdoc />
        public bool TrySelect(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return false;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return true;
        }
    }
}
