using System.Collections.Generic;
using MasterDataTool.EditorTools.Importing;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// 保存を試みた結果です。
    /// </summary>
    internal sealed class PipelineSaveInfo
    {
        /// <summary>
        /// 保存結果を初期化します。
        /// </summary>
        /// <param name="outcome">保存の結果種別です。</param>
        /// <param name="assetPath">アセットパスです。</param>
        /// <param name="previousRecordCount">更新前の件数です。新規作成と失敗時は -1 です。</param>
        /// <param name="newRecordCount">保存後の件数です。失敗時は -1 です。</param>
        /// <param name="issues">保存時の問題です。</param>
        public PipelineSaveInfo(
            SaveOutcome outcome,
            string assetPath,
            int previousRecordCount,
            int newRecordCount,
            IReadOnlyList<SaveIssue> issues)
        {
            Outcome = outcome;
            AssetPath = assetPath;
            PreviousRecordCount = previousRecordCount;
            NewRecordCount = newRecordCount;
            Issues = issues ?? new SaveIssue[0];
        }

        /// <summary>保存の結果種別です。</summary>
        public SaveOutcome Outcome { get; }

        /// <summary>アセットパスです。</summary>
        public string AssetPath { get; }

        /// <summary>更新前の件数です。新規作成と失敗時は -1 です。</summary>
        public int PreviousRecordCount { get; }

        /// <summary>保存後の件数です。失敗時は -1 です。</summary>
        public int NewRecordCount { get; }

        /// <summary>保存時の問題です。</summary>
        public IReadOnlyList<SaveIssue> Issues { get; }
    }
}
