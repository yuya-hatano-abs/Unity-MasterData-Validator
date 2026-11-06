namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// マスタデータの検証器です。検証対象は <see cref="ValidationContext.Targets"/> です。
    /// <see cref="MasterTable"/> のレコードを変更してはなりません。
    /// </summary>
    public interface IValidator
    {
        /// <summary>検証器の識別子です。</summary>
        string Id { get; }

        /// <summary>
        /// 検証を実行します。
        /// </summary>
        /// <param name="context">検証文脈です。<see cref="ValidationContext.Tables"/> は参照先の検索に使います。</param>
        /// <returns>検証結果です。</returns>
        ValidationResult Validate(ValidationContext context);
    }
}
