using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataValidator.Samples
{
    /// <summary>アイテムのマスタアセットです。</summary>
    [CreateAssetMenu(fileName = "ItemMaster", menuName = "Master Data/Item")]
    public sealed class ItemMaster : MasterDataBase<ItemRecord>
    {
    }
}
