using System.Collections.Generic;
using UnityEngine;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// マスタアセット保存の結果です。
    /// </summary>
    /// <typeparam name="TMaster">マスタ ScriptableObject 型です。</typeparam>
    public sealed class MasterAssetSaveResult<TMaster>
        where TMaster : ScriptableObject
    {
        /// <summary>
        /// 保存結果を初期化します。
        /// </summary>
        public MasterAssetSaveResult(
            SaveOutcome outcome,
            TMaster asset,
            string assetPath,
            int previousRecordCount,
            int newRecordCount,
            IReadOnlyList<SaveIssue> issues)
        {
            Outcome = outcome;
            Asset = asset;
            AssetPath = assetPath;
            PreviousRecordCount = previousRecordCount;
            NewRecordCount = newRecordCount;
            Issues = issues;
            Succeeded = outcome != SaveOutcome.Failed;
        }

        /// <summary>Outcome が Failed でない場合に true です。</summary>
        public bool Succeeded { get; }

        /// <summary>保存結果種別です。</summary>
        public SaveOutcome Outcome { get; }

        /// <summary>保存されたアセットです。失敗時は null です。</summary>
        public TMaster Asset { get; }

        /// <summary>アセットパスです。</summary>
        public string AssetPath { get; }

        /// <summary>更新前のレコード件数です。新規作成と失敗時は -1 です。</summary>
        public int PreviousRecordCount { get; }

        /// <summary>保存後のレコード件数です。失敗時は -1 です。</summary>
        public int NewRecordCount { get; }

        /// <summary>問題一覧です。</summary>
        public IReadOnlyList<SaveIssue> Issues { get; }
    }
}
