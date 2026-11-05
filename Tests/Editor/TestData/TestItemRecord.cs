using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 整数 ID マスタのテスト用レコードです。
    /// </summary>
    [System.Serializable]
    public sealed class TestItemRecord : IntIdMasterRecord
    {
        [SerializeField]
        string name;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestItemRecord()
        {
        }

        /// <summary>ID と名前でレコードを初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        /// <param name="name">表示名です。</param>
        public TestItemRecord(int id, string name)
            : base(id)
        {
            this.name = name;
        }

        /// <summary>表示名です。</summary>
        public string Name => name;
    }
}
