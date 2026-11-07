using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEditor;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// プロジェクト内のマスタアセットを読み込みます。
    /// </summary>
    internal interface IMasterAssetSource
    {
        /// <summary>
        /// マスタアセットを読み込みます。
        /// </summary>
        /// <returns>読み込めたアセットです。</returns>
        IReadOnlyList<MasterDataBase> LoadAll();
    }

    /// <summary>
    /// AssetDatabase からマスタアセットを読み込みます。
    /// </summary>
    /// <remarks>
    /// <c>t:MasterDataBase</c> が派生型のアセットを返すかは、この環境では未確認です。
    /// そのため、基底型の検索に加えて、具象型の名前ごとの検索結果を合わせています。
    /// </remarks>
    internal sealed class AssetDatabaseMasterAssetSource : IMasterAssetSource
    {
        readonly string[] searchFolders;

        /// <summary>
        /// 検索範囲を指定して初期化します。
        /// </summary>
        /// <param name="searchFolders">検索するフォルダです。null の場合はプロジェクト全体です。</param>
        public AssetDatabaseMasterAssetSource(string[] searchFolders = null)
        {
            this.searchFolders = searchFolders;
        }

        /// <inheritdoc />
        public IReadOnlyList<MasterDataBase> LoadAll()
        {
            var guids = new HashSet<string>();
            AddGuids(guids, "t:MasterDataBase");

            var types = TypeCache.GetTypesDerivedFrom<MasterDataBase>();
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                AddGuids(guids, "t:" + type.Name);
            }

            var assets = new List<MasterDataBase>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MasterDataBase>(path);
                if (asset == null)
                {
                    continue;
                }

                assets.Add(asset);
            }

            assets.Sort((a, b) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(a),
                AssetDatabase.GetAssetPath(b)));
            return assets;
        }

        void AddGuids(HashSet<string> guids, string filter)
        {
            var found = searchFolders == null
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, searchFolders);
            for (var i = 0; i < found.Length; i++)
            {
                guids.Add(found[i]);
            }
        }
    }
}
