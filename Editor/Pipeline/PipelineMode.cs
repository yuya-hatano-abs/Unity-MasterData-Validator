namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// インポートパイプラインの実行モードです。
    /// </summary>
    internal enum PipelineMode
    {
        /// <summary>検証だけを行い、保存しません。</summary>
        Validate,

        /// <summary>検証し、エラーがなければ保存します。</summary>
        ValidateAndSave,
    }
}
