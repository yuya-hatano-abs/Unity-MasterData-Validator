using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// 標準の検証器を提供します。
    /// </summary>
    public static class DefaultValidators
    {
        /// <summary>
        /// 標準の検証器リストを返します。
        /// </summary>
        /// <returns>検証器のリストです。呼ぶたびに新しいリストと新しい検証器インスタンスを返します。</returns>
        public static IReadOnlyList<IValidator> Create()
        {
            return new List<IValidator>
            {
                new DuplicateIdValidator(),
            };
        }
    }
}
