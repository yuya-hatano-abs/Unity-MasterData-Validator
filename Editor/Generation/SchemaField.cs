namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// 推論された 1 つのフィールドを表します。
    /// </summary>
    public sealed class SchemaField
    {
        /// <summary>
        /// スキーマフィールドを初期化します。
        /// </summary>
        /// <param name="jsonName">JSON キー名です。</param>
        /// <param name="type">スカラー型です。</param>
        /// <param name="isList">配列フィールドかどうかです。</param>
        /// <param name="propertyName">公開プロパティ名です。</param>
        /// <param name="cSharpTypeName">C# の型名です。</param>
        public SchemaField(
            string jsonName,
            SchemaFieldType type,
            bool isList,
            string propertyName,
            string cSharpTypeName)
        {
            JsonName = jsonName;
            Type = type;
            IsList = isList;
            PropertyName = propertyName;
            CSharpTypeName = cSharpTypeName;
        }

        /// <summary>JSON キー名です。</summary>
        public string JsonName { get; }

        /// <summary>スカラー型です。</summary>
        public SchemaFieldType Type { get; }

        /// <summary>配列フィールドかどうかです。</summary>
        public bool IsList { get; }

        /// <summary>公開プロパティ名です。</summary>
        public string PropertyName { get; }

        /// <summary>C# の型名です。</summary>
        public string CSharpTypeName { get; }
    }
}
