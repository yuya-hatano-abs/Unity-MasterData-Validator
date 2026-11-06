using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// フィールド型が不正なテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_FloatFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestItemMaster))]
        float value;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_FloatFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_FloatFieldRecord(int id)
            : base(id)
        {
            value = 1.5f;
        }

        /// <summary>不正な参照フィールドです。</summary>
        public float Value => value;
    }

    /// <summary>
    /// 参照先がマスタでないテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_NonMasterTargetRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(string))]
        int itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_NonMasterTargetRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_NonMasterTargetRecord(int id)
            : base(id)
        {
            itemId = 999;
        }

        /// <summary>参照フィールドです。</summary>
        public int ItemId => itemId;
    }

    /// <summary>
    /// 参照先が abstract なテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_AbstractTargetRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestAbstractMaster))]
        int itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_AbstractTargetRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_AbstractTargetRecord(int id)
            : base(id)
        {
            itemId = 999;
        }

        /// <summary>参照フィールドです。</summary>
        public int ItemId => itemId;
    }

    /// <summary>
    /// 参照先型が null のテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_NullTargetRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(null)]
        int itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_NullTargetRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_NullTargetRecord(int id)
            : base(id)
        {
            itemId = 999;
        }

        /// <summary>参照フィールドです。</summary>
        public int ItemId => itemId;
    }

    /// <summary>
    /// int フィールドが string ID マスタを参照するテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_IntToStringMasterRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestQuestMaster))]
        int questId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_IntToStringMasterRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_IntToStringMasterRecord(int id)
            : base(id)
        {
            questId = 999;
        }

        /// <summary>参照フィールドです。</summary>
        public int QuestId => questId;
    }

    /// <summary>
    /// string フィールドが int ID マスタを参照するテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_StringToIntMasterRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestItemMaster))]
        string itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_StringToIntMasterRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_StringToIntMasterRecord(int id)
            : base(id)
        {
            itemId = "999";
        }

        /// <summary>参照フィールドです。</summary>
        public string ItemId => itemId;
    }

    /// <summary>
    /// string フィールドに NoneValue を指定したテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_StringNoneValueRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestQuestMaster), NoneValue = 0)]
        string questId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_StringNoneValueRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_StringNoneValueRecord(int id)
            : base(id)
        {
            questId = "missing";
        }

        /// <summary>参照フィールドです。</summary>
        public string QuestId => questId;
    }

    /// <summary>
    /// シリアライズされないフィールドに [MasterRef] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_NonSerializedFieldRecord : IntIdMasterRecord
    {
        [NonSerialized, MasterRef(typeof(TestItemMaster))]
        int itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_NonSerializedFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_NonSerializedFieldRecord(int id)
            : base(id)
        {
            itemId = 999;
        }

        /// <summary>参照フィールドです。</summary>
        public int ItemId => itemId;
    }

    /// <summary>
    /// 型が不正で、かつ string 系に NoneValue を指定したテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRef_InvalidTypeAndStringNoneValueRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestQuestMaster), NoneValue = 0)]
        IReadOnlyList<string> values;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRef_InvalidTypeAndStringNoneValueRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRef_InvalidTypeAndStringNoneValueRecord(int id)
            : base(id)
        {
            values = new List<string> { "missing" };
        }

        /// <summary>参照フィールドです。</summary>
        public IReadOnlyList<string> Values => values;
    }

    /// <summary>
    /// 定義の問題とレコードの参照切れが混在する順序確認用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestMixedDefAndRefRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRef(typeof(TestItemMaster))]
        float bad;

        [SerializeField, MasterRef(typeof(TestItemMaster))]
        int itemId;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestMixedDefAndRefRecord()
        {
        }

        /// <summary>
        /// ID とアイテム参照で初期化します。
        /// </summary>
        /// <param name="id">レコード ID です。</param>
        /// <param name="itemId">アイテム参照です。</param>
        public TestMixedDefAndRefRecord(int id, int itemId)
            : base(id)
        {
            bad = 1.5f;
            this.itemId = itemId;
        }

        /// <summary>不正な参照フィールドです。</summary>
        public float Bad => bad;

        /// <summary>アイテム参照です。</summary>
        public int ItemId => itemId;
    }
}
