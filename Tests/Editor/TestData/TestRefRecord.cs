using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 参照フィールドを持つテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestRefRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestItemMaster))]
        int itemId;

        [SerializeField, MasterRef(typeof(TestItemMaster), NoneValue = 0)]
        int optionalItemId;

        [SerializeField, MasterRef(typeof(TestQuestMaster))]
        string questId;

        [SerializeField, MasterRef(typeof(TestItemMaster))]
        List<int> itemIds;

        [SerializeField, MasterRef(typeof(TestQuestMaster))]
        string[] questIds;

        [SerializeField]
        int plainNumber;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestRefRecord()
        {
        }

        /// <summary>
        /// 参照フィールドを指定してレコードを初期化します。
        /// </summary>
        public TestRefRecord(
            int id,
            int itemId,
            int optionalItemId,
            string questId,
            List<int> itemIds,
            string[] questIds,
            int plainNumber)
            : base(id)
        {
            this.itemId = itemId;
            this.optionalItemId = optionalItemId;
            this.questId = questId;
            this.itemIds = itemIds;
            this.questIds = questIds;
            this.plainNumber = plainNumber;
        }

        /// <summary>必須のアイテム参照です。</summary>
        public int ItemId => itemId;

        /// <summary>省略可能なアイテム参照です。</summary>
        public int OptionalItemId => optionalItemId;

        /// <summary>クエスト参照です。</summary>
        public string QuestId => questId;

        /// <summary>アイテム参照の一覧です。</summary>
        public List<int> ItemIds => itemIds;

        /// <summary>クエスト参照の配列です。</summary>
        public string[] QuestIds => questIds;

        /// <summary>参照ではない数値です。</summary>
        public int PlainNumber => plainNumber;
    }
}
