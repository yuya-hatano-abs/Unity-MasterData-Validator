using System;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Tests.NameClashB
{
    /// <summary>
    /// 名前衝突の確認用レコードです。
    /// </summary>
    [Serializable]
    public sealed class ClashRecord : IntIdMasterRecord
    {
        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public ClashRecord()
        {
        }

        /// <summary>
        /// ID でレコードを初期化します。
        /// </summary>
        /// <param name="id">レコード ID です。</param>
        public ClashRecord(int id)
            : base(id)
        {
        }
    }
}
