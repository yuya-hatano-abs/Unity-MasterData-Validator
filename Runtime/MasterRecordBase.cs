using System;
using System.Globalization;
using UnityEngine;

namespace MasterDataTool.Runtime
{
    /// <summary>
    /// マスタレコードの抽象基底クラスです。外部から直接継承できません。
    /// </summary>
    [Serializable]
    public abstract class MasterRecordBase : IMasterRecord
    {
        /// <summary>派生型専用の internal コンストラクタです。</summary>
        internal MasterRecordBase()
        {
        }

        /// <inheritdoc />
        public MasterIdKind IdKind => IdKindImpl;

        /// <inheritdoc />
        public string Key => KeyImpl;

        /// <summary>ID 種別の実装です。</summary>
        internal abstract MasterIdKind IdKindImpl { get; }

        /// <summary>正規化キーの実装です。</summary>
        internal abstract string KeyImpl { get; }
    }

    /// <summary>
    /// 整数 ID を持つマスタレコードの基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class IntIdMasterRecord : MasterRecordBase
    {
        [SerializeField]
        private int id;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        protected IntIdMasterRecord()
        {
        }

        /// <summary>指定した ID でレコードを初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        protected IntIdMasterRecord(int id)
        {
            this.id = id;
        }

        /// <summary>整数 ID です。</summary>
        public int Id => id;

        /// <inheritdoc />
        internal override MasterIdKind IdKindImpl => MasterIdKind.Int;

        /// <inheritdoc />
        internal override string KeyImpl => id.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 文字列 ID を持つマスタレコードの基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class StringIdMasterRecord : MasterRecordBase
    {
        [SerializeField]
        private string id;

        /// <summary>シリアライズ用の既定コンストラクタです。</summary>
        protected StringIdMasterRecord()
        {
        }

        /// <summary>指定した ID でレコードを初期化します。</summary>
        /// <param name="id">レコード ID です。</param>
        protected StringIdMasterRecord(string id)
        {
            this.id = id;
        }

        /// <summary>文字列 ID です。null は空文字として扱います。</summary>
        public string Id => id ?? string.Empty;

        /// <inheritdoc />
        internal override MasterIdKind IdKindImpl => MasterIdKind.String;

        /// <inheritdoc />
        internal override string KeyImpl => Id;
    }
}
