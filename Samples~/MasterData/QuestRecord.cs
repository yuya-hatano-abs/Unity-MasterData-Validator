using System;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataValidator.Samples
{
    /// <summary>クエスト 1 件です。ID は文字列で、報酬はアイテム ID です。</summary>
    [Serializable]
    public sealed class QuestRecord : StringIdMasterRecord
    {
        [SerializeField, MasterRequired] private string title;
        [SerializeField, MasterRef(typeof(ItemMaster), NoneValue = 0)] private int rewardItemId;

        public string Title => title;
        public int RewardItemId => rewardItemId;
    }
}
