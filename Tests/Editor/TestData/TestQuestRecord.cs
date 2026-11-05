using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 文字列 ID マスタのテスト用レコードです。
    /// </summary>
    [System.Serializable]
    public sealed class TestQuestRecord : StringIdMasterRecord
    {
        [SerializeField]
        string title;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestQuestRecord()
        {
        }

        /// <summary>ID とタイトルでレコードを初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        /// <param name="title">タイトルです。</param>
        public TestQuestRecord(string id, string title)
            : base(id)
        {
            this.title = title;
        }

        /// <summary>タイトルです。</summary>
        public string Title => title;
    }
}
