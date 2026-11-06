using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 必須フィールドを持つテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestRequiredRecord : StringIdMasterRecord
    {
        [SerializeField, MasterRequired]
        string name;

        [SerializeField, MasterRequired]
        List<int> dropItemIds;

        [SerializeField, MasterRequired]
        string[] tags;

        [SerializeField]
        string note;

        [SerializeField, MasterRequired, MasterRef(typeof(TestQuestMaster))]
        string questId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestRequiredRecord()
        {
        }

        /// <summary>
        /// 必須フィールドを指定してレコードを初期化します。
        /// </summary>
        public TestRequiredRecord(
            string id,
            string name,
            List<int> dropItemIds,
            string[] tags,
            string note,
            string questId)
            : base(id)
        {
            this.name = name;
            this.dropItemIds = dropItemIds;
            this.tags = tags;
            this.note = note;
            this.questId = questId;
        }

        /// <summary>必須の名前です。</summary>
        public string Name => name;

        /// <summary>必須のドロップ ID 一覧です。</summary>
        public List<int> DropItemIds => dropItemIds;

        /// <summary>必須のタグ配列です。</summary>
        public string[] Tags => tags;

        /// <summary>必須ではないメモです。</summary>
        public string Note => note;

        /// <summary>必須かつ参照のクエスト ID です。</summary>
        public string QuestId => questId;
    }
}
