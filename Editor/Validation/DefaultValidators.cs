using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// 標準の検証器を提供します。
    /// </summary>
    public static class DefaultValidators
    {
        /// <summary>
        /// 標準の検証器リストを返します。Task 7 以降で項目が追加されます。
        /// </summary>
        /// <returns>検証器のリストです。呼ぶたびに新しいリストを返します。</returns>
        public static IReadOnlyList<IValidator> Create()
        {
            return new List<IValidator>();
        }
    }
}
