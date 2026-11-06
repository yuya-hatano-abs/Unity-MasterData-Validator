namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// マスタアセット保存の結果種別です。
    /// </summary>
    public enum SaveOutcome
    {
        /// <summary>新規アセットを作成しました。</summary>
        Created,

        /// <summary>既存アセットを更新しました。</summary>
        Updated,

        /// <summary>内容に変更がなかったため、書き込みませんでした。</summary>
        Unchanged,

        /// <summary>保存に失敗しました。</summary>
        Failed,
    }
}
