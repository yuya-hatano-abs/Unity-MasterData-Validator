namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// バリデーション問題の種類です。
    /// </summary>
    public enum ValidationIssueKind
    {
        /// <summary>レコード ID が重複しています(Task 7)。</summary>
        DuplicateId,

        /// <summary>参照先が存在しません(Task 8)。</summary>
        BrokenReference,

        /// <summary>必須フィールドが欠けています(Task 9)。</summary>
        MissingRequiredField,

        /// <summary>インポート時の JSON パース・構造・型定義の問題です。</summary>
        ImportFailed,

        /// <summary>値の型が一致しません。</summary>
        TypeMismatch,

        /// <summary>値が範囲外です。</summary>
        ValueOutOfRange,

        /// <summary>未知のフィールドです。</summary>
        UnknownField,

        /// <summary>フィールドに対応する値がありません。</summary>
        MissingField,

        /// <summary>サポートされていないフィールド型です。</summary>
        UnsupportedFieldType,

        /// <summary>検証器の実行に失敗しました。</summary>
        ValidatorFailure,

        /// <summary>参照先のマスタが、検証の文脈(Tables)に含まれていません。</summary>
        ReferenceTargetUnavailable,

        /// <summary>[MasterRef] の指定が不正です(設定のミス)。</summary>
        InvalidReferenceDefinition,

        /// <summary>[MasterRequired] の指定が不正です(設定のミス)。</summary>
        InvalidRequiredDefinition,

        /// <summary>同じマスタ型のアセットが複数あるため、参照先として使えません。</summary>
        MultipleMasterAssets,

        /// <summary>同じ名前のマスタ型が複数あるため、参照先として使えません。</summary>
        AmbiguousMasterName,
    }
}
