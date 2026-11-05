using System;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// テスト用の Flags 列挙型です。
    /// </summary>
    [Flags]
    public enum TestFlagsOption
    {
        /// <summary>なしです。</summary>
        None = 0,

        /// <summary>オプション A です。</summary>
        A = 1,

        /// <summary>オプション B です。</summary>
        B = 2,

        /// <summary>オプション C です。</summary>
        C = 4
    }
}
