using System.Collections.Generic;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// JSON から推論されたマスタのスキーマです。
    /// </summary>
    public sealed class MasterSchema
    {
        /// <summary>
        /// スキーマを初期化します。
        /// </summary>
        /// <param name="idKind">レコード ID の種類です。</param>
        /// <param name="fields">id 以外のフィールド一覧です。</param>
        public MasterSchema(MasterIdKind idKind, IReadOnlyList<SchemaField> fields)
        {
            IdKind = idKind;
            Fields = fields;
        }

        /// <summary>レコード ID の種類です。</summary>
        public MasterIdKind IdKind { get; }

        /// <summary>id 以外のフィールド一覧です。</summary>
        public IReadOnlyList<SchemaField> Fields { get; }
    }
}
