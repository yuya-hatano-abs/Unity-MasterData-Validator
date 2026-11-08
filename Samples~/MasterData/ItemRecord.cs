using System;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataValidator.Samples
{
    /// <summary>アイテム 1 件です。ID は整数です。</summary>
    [Serializable]
    public sealed class ItemRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired] private string name;
        [SerializeField] private int price;

        public string Name => name;
        public int Price => price;
    }
}
