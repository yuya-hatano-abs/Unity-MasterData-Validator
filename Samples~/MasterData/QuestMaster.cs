using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataValidator.Samples
{
    /// <summary>クエストのマスタアセットです。</summary>
    [CreateAssetMenu(fileName = "QuestMaster", menuName = "Master Data/Quest")]
    public sealed class QuestMaster : MasterDataBase<QuestRecord>
    {
    }
}
