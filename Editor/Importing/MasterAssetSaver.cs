using System;
using System.Collections.Generic;
using System.IO;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.Runtime;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// インポート結果を ScriptableObject アセットとして保存します。
    /// </summary>
    public static class MasterAssetSaver
    {
        /// <summary>
        /// インポート結果をマスタアセットとして保存します。
        /// </summary>
        /// <typeparam name="TMaster">マスタ型です。</typeparam>
        /// <typeparam name="TRecord">レコード型です。</typeparam>
        /// <param name="importResult">インポート結果です。</param>
        /// <param name="assetPath">Assets/ で始まる .asset パスです。</param>
        /// <param name="options">保存オプションです。null の場合は既定値を使います。</param>
        /// <returns>保存結果です。</returns>
        public static MasterAssetSaveResult<TMaster> Save<TMaster, TRecord>(
            ImportResult<TRecord> importResult,
            string assetPath,
            MasterAssetSaveOptions options = null)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            if (importResult == null)
            {
                throw new ArgumentNullException(nameof(importResult));
            }

            if (options == null)
            {
                options = new MasterAssetSaveOptions();
            }

            var issues = new List<SaveIssue>();
            var normalizedPath = assetPath;

            if (typeof(TMaster).IsAbstract)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.InvalidMasterType,
                    "型 '" + typeof(TMaster).Name + "' は abstract のため、アセットを作成できません。"));
                return Fail<TMaster>(normalizedPath, issues);
            }

            if (!importResult.Succeeded)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.ImportNotSucceeded,
                    "インポートが成功していないため、保存できません。"));
                return Fail<TMaster>(normalizedPath, issues);
            }

            if (importResult.HasErrors)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.ImportHasErrors,
                    "インポート結果に Error が含まれるため、保存できません。"));
                return Fail<TMaster>(normalizedPath, issues);
            }

            if (!ValidateAssetPath(assetPath, issues))
            {
                return Fail<TMaster>(assetPath, issues);
            }

            normalizedPath = assetPath.Replace('\\', '/');
            var newRecordCount = importResult.Records.Count;
            var existingType = AssetDatabase.GetMainAssetTypeAtPath(normalizedPath);

            if (existingType == null)
            {
                return CreateNewAsset<TMaster, TRecord>(importResult, normalizedPath, options, newRecordCount, issues);
            }

            if (!typeof(TMaster).IsAssignableFrom(existingType))
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.AssetTypeMismatch,
                    "パス '" + normalizedPath + "' に、型 '" + existingType.Name + "' のアセットがあり、'" +
                    typeof(TMaster).Name + "' とは互換性がありません。"));
                return Fail<TMaster>(normalizedPath, issues);
            }

            if (options.ExistingAsset == OverwritePolicy.Fail)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.AssetExists,
                    "アセットが既に存在します: " + normalizedPath));
                return Fail<TMaster>(normalizedPath, issues);
            }

            return UpdateExistingAsset<TMaster, TRecord>(importResult, normalizedPath, options, newRecordCount, issues);
        }

        static MasterAssetSaveResult<TMaster> CreateNewAsset<TMaster, TRecord>(
            ImportResult<TRecord> importResult,
            string assetPath,
            MasterAssetSaveOptions options,
            int newRecordCount,
            List<SaveIssue> issues)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            if (!EnsureAssetFolders(assetPath, options.CreateFolders, issues))
            {
                return Fail<TMaster>(assetPath, issues);
            }

            var instance = ScriptableObject.CreateInstance<TMaster>();
            try
            {
                instance.ReplaceRecords(importResult.Records);
                AssetDatabase.CreateAsset(instance, assetPath);
            }
            catch (Exception ex)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                issues.Add(new SaveIssue(
                    SaveIssueKind.SaveFailed,
                    "アセットの作成に失敗しました: " + ex.Message));
                return Fail<TMaster>(assetPath, issues);
            }

            var saved = AssetDatabase.LoadAssetAtPath<TMaster>(assetPath);
            if (saved == null)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.SaveFailed,
                    "作成したアセットを読み込めません: " + assetPath));
                return Fail<TMaster>(assetPath, issues);
            }

            return new MasterAssetSaveResult<TMaster>(
                SaveOutcome.Created,
                saved,
                assetPath,
                previousRecordCount: -1,
                newRecordCount,
                issues);
        }

        static MasterAssetSaveResult<TMaster> UpdateExistingAsset<TMaster, TRecord>(
            ImportResult<TRecord> importResult,
            string assetPath,
            MasterAssetSaveOptions options,
            int newRecordCount,
            List<SaveIssue> issues)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMaster>(assetPath);
            if (existing == null)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.AssetTypeMismatch,
                    "パス '" + assetPath + "' から、型 '" + typeof(TMaster).Name +
                    "' のアセットを読み込めません。"));
                return Fail<TMaster>(assetPath, issues);
            }

            var previousRecordCount = existing.Count;
            var comparisonInstance = ScriptableObject.CreateInstance<TMaster>();
            try
            {
                comparisonInstance.ReplaceRecords(importResult.Records);
                if (RecordsSerializedEquals<TMaster, TRecord>(existing, comparisonInstance))
                {
                    return new MasterAssetSaveResult<TMaster>(
                        SaveOutcome.Unchanged,
                        existing,
                        assetPath,
                        previousRecordCount,
                        newRecordCount,
                        issues);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(comparisonInstance);
            }

            try
            {
                existing.ReplaceRecords(importResult.Records);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
            }
            catch (Exception ex)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.SaveFailed,
                    "アセットの更新に失敗しました: " + ex.Message));
                return Fail<TMaster>(assetPath, issues);
            }

            var reloaded = AssetDatabase.LoadAssetAtPath<TMaster>(assetPath);
            return new MasterAssetSaveResult<TMaster>(
                SaveOutcome.Updated,
                reloaded != null ? reloaded : existing,
                assetPath,
                previousRecordCount,
                newRecordCount,
                issues);
        }

        /// <summary>
        /// records フィールドのみを SerializedProperty.DataEquals で比較します。
        /// ScriptableObject 本体の m_Name 等の差を除外するため、JsonUtility 全体比較は使いません。
        /// </summary>
        internal static bool RecordsSerializedEquals<TMaster, TRecord>(TMaster left, TMaster right)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            using (var leftObject = new SerializedObject(left))
            using (var rightObject = new SerializedObject(right))
            {
                var leftRecords = leftObject.FindProperty("records");
                var rightRecords = rightObject.FindProperty("records");
                if (leftRecords == null || rightRecords == null)
                {
                    return false;
                }

                return SerializedProperty.DataEquals(leftRecords, rightRecords);
            }
        }

        static bool EnsureAssetFolders(string assetPath, bool createFolders, List<SaveIssue> issues)
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(folderPath))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスからフォルダを特定できません。"));
                return false;
            }

            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return true;
            }

            if (!createFolders)
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.FolderNotFound,
                    "フォルダが存在しません: " + folderPath));
                return false;
            }

            var segments = folderPath.Split('/');
            if (segments.Length == 0 || !string.Equals(segments[0], "Assets", StringComparison.Ordinal))
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.FolderCreationFailed,
                    "フォルダを作成できません: " + folderPath));
                return false;
            }

            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, segments[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        issues.Add(new SaveIssue(
                            SaveIssueKind.FolderCreationFailed,
                            "フォルダを作成できません: " + next));
                        return false;
                    }
                }

                current = next;
            }

            return AssetDatabase.IsValidFolder(folderPath);
        }

        static bool ValidateAssetPath(string assetPath, List<SaveIssue> issues)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスが空です。"));
                return false;
            }

            if (Path.IsPathRooted(assetPath))
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.InvalidAssetPath,
                    "アセットパスは Assets/ で始まるプロジェクト相対パスである必要があります。"));
                return false;
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                issues.Add(new SaveIssue(
                    SaveIssueKind.InvalidAssetPath,
                    "アセットパスは Assets/ で始まる必要があります。"));
                return false;
            }

            if (assetPath.Contains("\\"))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスにバックスラッシュを含めることはできません。"));
                return false;
            }

            if (assetPath.EndsWith("/", StringComparison.Ordinal))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスをスラッシュで終えることはできません。"));
                return false;
            }

            if (assetPath.IndexOf("//", StringComparison.Ordinal) >= 0)
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスに連続したスラッシュを含めることはできません。"));
                return false;
            }

            if (!assetPath.EndsWith(".asset", StringComparison.Ordinal))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスは .asset で終わる必要があります。"));
                return false;
            }

            var fileName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(fileName) ||
                string.Equals(fileName, ".asset", StringComparison.Ordinal))
            {
                issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットのファイル名が空です。"));
                return false;
            }

            var segments = assetPath.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[i]))
                {
                    issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスに空のセグメントが含まれています。"));
                    return false;
                }

                if (string.Equals(segments[i], "..", StringComparison.Ordinal))
                {
                    issues.Add(new SaveIssue(SaveIssueKind.InvalidAssetPath, "アセットパスに .. を含めることはできません。"));
                    return false;
                }
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < assetPath.Length; i++)
            {
                var c = assetPath[i];
                if (c == '/')
                {
                    continue;
                }

                for (var j = 0; j < invalidChars.Length; j++)
                {
                    if (c == invalidChars[j])
                    {
                        issues.Add(new SaveIssue(
                            SaveIssueKind.InvalidAssetPath,
                            "アセットパスに使用できない文字が含まれています。"));
                        return false;
                    }
                }
            }

            return true;
        }

        static MasterAssetSaveResult<TMaster> Fail<TMaster>(string assetPath, List<SaveIssue> issues)
            where TMaster : ScriptableObject
        {
            return new MasterAssetSaveResult<TMaster>(
                SaveOutcome.Failed,
                null,
                assetPath,
                previousRecordCount: -1,
                newRecordCount: -1,
                issues);
        }
    }
}
