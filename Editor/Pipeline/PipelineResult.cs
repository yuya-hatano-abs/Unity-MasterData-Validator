using MasterDataTool.EditorTools.Diff;
using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// インポートパイプラインの 1 回の結果です。
    /// </summary>
    internal sealed class PipelineResult
    {
        /// <summary>
        /// 結果を初期化します。
        /// </summary>
        /// <param name="status">終了状態です。</param>
        /// <param name="validation">結合した検証結果です。null は空の結果になります。</param>
        /// <param name="table">今回インポートしたテーブルです。</param>
        /// <param name="canSave">インポートが成功し、検証エラーがない場合に true です。</param>
        /// <param name="save">保存を試みた結果です。試していない場合は null です。</param>
        /// <param name="message">依頼不正または想定外の例外の説明です。</param>
        /// <param name="diff">既存アセットとの差分です。無い場合は null です。</param>
        public PipelineResult(
            PipelineStatus status,
            ValidationResult validation,
            MasterTable table,
            bool canSave,
            PipelineSaveInfo save,
            string message,
            MasterDiffReport diff = null)
        {
            Status = status;
            Validation = validation ?? ValidationResult.Empty;
            Table = table;
            CanSave = canSave;
            Save = save;
            Message = message ?? string.Empty;
            Diff = diff;
        }

        /// <summary>終了状態です。</summary>
        public PipelineStatus Status { get; }

        /// <summary>結合した検証結果です。</summary>
        public ValidationResult Validation { get; }

        /// <summary>今回インポートしたテーブルです。依頼不正と想定外の例外では null です。</summary>
        public MasterTable Table { get; }

        /// <summary>インポートが成功し、検証エラーがない場合に true です。</summary>
        public bool CanSave { get; }

        /// <summary>保存を試みた結果です。試していない場合と、キャンセル時は null です。</summary>
        public PipelineSaveInfo Save { get; }

        /// <summary>依頼不正または想定外の例外の説明です。それ以外は空文字です。</summary>
        public string Message { get; }

        /// <summary>既存アセットとの差分です。無い場合は null です。</summary>
        public MasterDiffReport Diff { get; }
    }
}
