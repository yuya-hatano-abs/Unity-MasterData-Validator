using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// EditMode テスト用の Assets 一時フォルダを管理します。
    /// </summary>
    public static class TempAssetFolder
    {
        /// <summary>一時アセットのルートフォルダです。</summary>
        public const string RootFolder = "Assets/__MasterDataToolTests__";

        /// <summary>
        /// 前回のテスト実行で残ったルートフォルダを削除します。
        /// </summary>
        public static void CleanupRootIfPresent()
        {
            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
            }
        }

        /// <summary>
        /// 一意な子フォルダを作成し、その Assets パスを返します。
        /// </summary>
        /// <returns>Assets/__MasterDataToolTests__/&lt;GUID&gt; 形式のパスです。</returns>
        public static string CreateUniqueFolder()
        {
            EnsureRootExists();
            var folderName = Guid.NewGuid().ToString("N");
            var createdGuid = AssetDatabase.CreateFolder(RootFolder, folderName);
            if (string.IsNullOrEmpty(createdGuid))
            {
                throw new InvalidOperationException("一時フォルダを作成できません: " + RootFolder + "/" + folderName);
            }

            return RootFolder + "/" + folderName;
        }

        /// <summary>
        /// 一時フォルダとその中身を削除します。
        /// </summary>
        /// <param name="folderPath">CreateUniqueFolder が返したパスです。</param>
        public static void DeleteFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        /// <summary>
        /// 一時フォルダ内のアセットパスを組み立てます。
        /// </summary>
        /// <param name="folderPath">一時フォルダの Assets パスです。</param>
        /// <param name="assetFileName">ファイル名(例: Item.asset)です。</param>
        /// <returns>Assets パスです。</returns>
        public static string AssetPath(string folderPath, string assetFileName)
        {
            return folderPath + "/" + assetFileName;
        }

        static void EnsureRootExists()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                var guid = AssetDatabase.CreateFolder("Assets", "__MasterDataToolTests__");
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException("テスト用ルートフォルダを作成できません: " + RootFolder);
                }
            }
        }
    }
}
