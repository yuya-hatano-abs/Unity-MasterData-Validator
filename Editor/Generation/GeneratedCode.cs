namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// 生成された C# ソース 2 ファイル分の内容です。
    /// </summary>
    public sealed class GeneratedCode
    {
        /// <summary>
        /// 生成コードを初期化します。
        /// </summary>
        public GeneratedCode(
            string recordFileName,
            string recordSource,
            string masterFileName,
            string masterSource,
            string recordClassName,
            string masterClassName)
        {
            RecordFileName = recordFileName;
            RecordSource = recordSource;
            MasterFileName = masterFileName;
            MasterSource = masterSource;
            RecordClassName = recordClassName;
            MasterClassName = masterClassName;
        }

        /// <summary>レコード型のファイル名です。</summary>
        public string RecordFileName { get; }

        /// <summary>レコード型のソースです。</summary>
        public string RecordSource { get; }

        /// <summary>マスタ型のファイル名です。</summary>
        public string MasterFileName { get; }

        /// <summary>マスタ型のソースです。</summary>
        public string MasterSource { get; }

        /// <summary>レコード型のクラス名です。</summary>
        public string RecordClassName { get; }

        /// <summary>マスタ型のクラス名です。</summary>
        public string MasterClassName { get; }
    }
}
