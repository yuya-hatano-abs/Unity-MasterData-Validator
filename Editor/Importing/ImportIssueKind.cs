namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// インポート問題の種類です。
    /// </summary>
    public enum ImportIssueKind
    {
        /// <summary>JSON の構文または形式が不正です。</summary>
        InvalidJson,

        /// <summary>ファイルが見つからない、または読み込めません。</summary>
        FileNotFound,

        /// <summary>ルートに records 配列がありません。</summary>
        MissingRecordsArray,

        /// <summary>records の要素がオブジェクトではありません。</summary>
        RecordNotObject,

        /// <summary>レコード型をインスタンス化できません。</summary>
        UnsupportedRecordType,

        /// <summary>シリアライズ対象フィールド名が継承チェーン内で重複しています。</summary>
        DuplicateFieldName,

        /// <summary>JSON の値型がフィールド型と一致しません。</summary>
        TypeMismatch,

        /// <summary>値がフィールド型の範囲外です。</summary>
        ValueOutOfRange,

        /// <summary>JSON に存在するが、フィールドに対応しないキーです。</summary>
        UnknownField,

        /// <summary>フィールドに対応する JSON キーがありません。</summary>
        MissingField,

        /// <summary>サポートされていないフィールド型です。</summary>
        UnsupportedFieldType
    }
}
