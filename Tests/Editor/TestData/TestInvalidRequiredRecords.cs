using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// int に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_IntFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        int value;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_IntFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_IntFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public int Value => value;
    }

    /// <summary>
    /// bool に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_BoolFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        bool flag;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_BoolFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_BoolFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public bool Flag => flag;
    }

    /// <summary>
    /// float に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_FloatFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        float ratio;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_FloatFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_FloatFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public float Ratio => ratio;
    }

    /// <summary>
    /// enum に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_EnumFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        TestRarity rarity;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_EnumFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_EnumFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public TestRarity Rarity => rarity;
    }

    /// <summary>
    /// 入れ子クラスに [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_NestedFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        TestNestedValue nested;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_NestedFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_NestedFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public TestNestedValue Nested => nested;
    }

    /// <summary>
    /// UnityEngine.Object に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_UnityObjectFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        UnityEngine.Object asset;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_UnityObjectFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_UnityObjectFieldRecord(int id)
            : base(id)
        {
        }

        /// <summary>不正な必須フィールドです。</summary>
        public UnityEngine.Object Asset => asset;
    }

    /// <summary>
    /// シリアライズされない string に [MasterRequired] を付けたテスト用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_NonSerializedFieldRecord : IntIdMasterRecord
    {
        [NonSerialized, MasterRequired]
        string hidden;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_NonSerializedFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_NonSerializedFieldRecord(int id)
            : base(id)
        {
            hidden = null;
        }

        /// <summary>不正な必須フィールドです。</summary>
        public string Hidden => hidden;
    }

    /// <summary>
    /// IReadOnlyList&lt;string&gt; に [MasterRequired] を付けたテスト用レコードです。
    /// SerializedFieldMap に載るため、型が対応していない定義の問題として報告されます。
    /// </summary>
    [Serializable]
    public sealed class TestInvalidRequired_ReadOnlyListFieldRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        IReadOnlyList<string> values;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestInvalidRequired_ReadOnlyListFieldRecord()
        {
        }

        /// <summary>ID で初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        public TestInvalidRequired_ReadOnlyListFieldRecord(int id)
            : base(id)
        {
            values = new List<string>();
        }

        /// <summary>不正な必須フィールドです。</summary>
        public IReadOnlyList<string> Values => values;
    }

    /// <summary>
    /// 定義の問題と空の必須フィールドが混在する順序確認用レコードです。
    /// </summary>
    [Serializable]
    public sealed class TestMixedRequiredRecord : IntIdMasterRecord
    {
        [SerializeField, MasterRequired]
        int bad;

        [SerializeField, MasterRequired]
        string name;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        public TestMixedRequiredRecord()
        {
        }

        /// <summary>
        /// ID と名前で初期化します。
        /// </summary>
        /// <param name="id">レコード ID です。</param>
        /// <param name="name">名前です。</param>
        public TestMixedRequiredRecord(int id, string name)
            : base(id)
        {
            this.name = name;
        }

        /// <summary>不正な必須フィールドです。</summary>
        public int Bad => bad;

        /// <summary>必須の名前です。</summary>
        public string Name => name;
    }
}
