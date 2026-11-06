namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// 推論されたスキーマフィールドのスカラー型です。
    /// </summary>
    public enum SchemaFieldType
    {
        /// <summary>32 ビット整数です。</summary>
        Int,

        /// <summary>64 ビット整数です。</summary>
        Long,

        /// <summary>単精度浮動小数点数です。</summary>
        Float,

        /// <summary>真偽値です。</summary>
        Bool,

        /// <summary>文字列です。</summary>
        String,
    }
}
