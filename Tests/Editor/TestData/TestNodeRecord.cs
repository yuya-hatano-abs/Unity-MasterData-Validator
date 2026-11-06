using System;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 自己参照を持つテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestNodeRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestNodeMaster), NoneValue = 0)]
        int parentId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestNodeRecord()
        {
        }

        /// <summary>
        /// ID と親 ID でレコードを初期化します。
        /// </summary>
        /// <param name="id">レコード ID です。</param>
        /// <param name="parentId">親ノードの ID です。</param>
        public TestNodeRecord(int id, int parentId)
            : base(id)
        {
            this.parentId = parentId;
        }

        /// <summary>親ノードの ID です。</summary>
        public int ParentId => parentId;
    }
}
