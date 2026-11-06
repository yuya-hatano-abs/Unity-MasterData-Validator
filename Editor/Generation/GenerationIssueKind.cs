namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// コード生成・スキーマ推論で発生する問題の種類です。
    /// </summary>
    public enum GenerationIssueKind
    {
        /// <summary>JSON のパースまたは構造の問題です。</summary>
        SourceError,

        /// <summary>records 配列が空です。</summary>
        EmptyRecords,

        /// <summary>id キーが欠けています。</summary>
        MissingId,

        /// <summary>id キーの型がレコード間で一致しません。</summary>
        InconsistentIdType,

        /// <summary>整数 id が int の範囲外です。</summary>
        IdOutOfRange,

        /// <summary>フィールド名が命名規則に適合しません。</summary>
        InvalidFieldName,

        /// <summary>プロパティ名が予約済みです。</summary>
        ReservedFieldName,

        /// <summary>フィールドの型がレコード間で一致しません。</summary>
        InconsistentFieldType,

        /// <summary>マスタ名が不正です。</summary>
        InvalidMasterName,

        /// <summary>名前空間が不正です。</summary>
        InvalidNamespace,

        /// <summary>出力パスが不正です。</summary>
        InvalidOutputPath,

        /// <summary>出力先に同名ファイルが既に存在します。</summary>
        FileExists,

        /// <summary>入出力エラーです。</summary>
        IoError,

        /// <summary>サポートされないフィールド値です。</summary>
        UnsupportedFieldValue,
    }
}
