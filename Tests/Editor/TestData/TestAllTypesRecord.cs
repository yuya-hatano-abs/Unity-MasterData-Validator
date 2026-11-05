using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 全フィールド型のインポートテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestAllTypesRecord : IntIdMasterRecord
    {
        [SerializeField]
        int count;

        [SerializeField]
        long bigCount;

        [SerializeField]
        float ratio;

        [SerializeField]
        double precise;

        [SerializeField]
        bool enabled;

        [SerializeField]
        string label;

        [SerializeField]
        TestRarity rarity;

        [SerializeField]
        TestFlagsOption options;

        [SerializeField]
        List<string> tags;

        [SerializeField]
        int[] values;

        [SerializeField]
        TestNestedValue nested;

        /// <summary>ID のみ指定するコンストラクタです。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestAllTypesRecord(int id)
            : base(id)
        {
        }

        /// <summary>count です。</summary>
        public int Count => count;

        /// <summary>bigCount です。</summary>
        public long BigCount => bigCount;

        /// <summary>ratio です。</summary>
        public float Ratio => ratio;

        /// <summary>precise です。</summary>
        public double Precise => precise;

        /// <summary>enabled です。</summary>
        public bool Enabled => enabled;

        /// <summary>label です。</summary>
        public string Label => label;

        /// <summary>rarity です。</summary>
        public TestRarity Rarity => rarity;

        /// <summary>options です。</summary>
        public TestFlagsOption Options => options;

        /// <summary>tags です。</summary>
        public IReadOnlyList<string> Tags => tags;

        /// <summary>values です。</summary>
        public int[] Values => values;

        /// <summary>nested です。</summary>
        public TestNestedValue Nested => nested;
    }
}
