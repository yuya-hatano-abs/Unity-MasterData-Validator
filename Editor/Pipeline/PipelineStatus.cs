namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// インポートパイプラインの終了状態です。
    /// </summary>
    internal enum PipelineStatus
    {
        /// <summary>依頼の内容が不正です。</summary>
        InvalidRequest,

        /// <summary>JSON の読み込みに失敗しました。</summary>
        ImportFailed,

        /// <summary>インポートは成功しましたが、検証にエラーがあります。</summary>
        ValidationFailed,

        /// <summary>検証が終わり、エラーはありません。保存はしていません。</summary>
        Validated,

        /// <summary>保存に成功しました。</summary>
        Saved,

        /// <summary>上書き確認で保存をキャンセルしました。</summary>
        SaveCancelled,

        /// <summary>保存に失敗しました。</summary>
        SaveFailed,

        /// <summary>想定外の例外が発生しました。</summary>
        UnexpectedError,
    }
}
