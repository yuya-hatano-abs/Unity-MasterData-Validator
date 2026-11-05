using System;
using System.Collections.Generic;
using System.Globalization;

namespace MasterDataTool.Runtime
{
    /// <summary>
    /// 1 マスタテーブル分の ScriptableObject の非ジェネリック基底クラスです。
    /// </summary>
    public abstract class MasterDataBase : UnityEngine.ScriptableObject
    {
        /// <summary>レコード型です。</summary>
        public abstract Type RecordType { get; }

        /// <summary>このテーブルの ID 種別です。</summary>
        public abstract MasterIdKind IdKind { get; }

        /// <summary>レコード件数です。</summary>
        public abstract int Count { get; }

        /// <summary>全レコードを読み取り専用で取得します。</summary>
        public abstract IReadOnlyList<IMasterRecord> AllRecords { get; }

        /// <summary>
        /// 正規化キーでレコードを検索します。
        /// </summary>
        /// <param name="key">検索キーです。</param>
        /// <param name="record">見つかったレコードです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public abstract bool TryGetRecordByKey(string key, out IMasterRecord record);
    }

    /// <summary>
    /// 型付きレコードリストを持つマスタ ScriptableObject の基底クラスです。
    /// </summary>
    /// <typeparam name="TRecord">レコード型です。</typeparam>
    public abstract class MasterDataBase<TRecord> : MasterDataBase
        where TRecord : MasterRecordBase
    {
        static readonly MasterIdKind CachedIdKind = ResolveIdKind(typeof(TRecord));

        [NonSerialized]
        Dictionary<string, TRecord> lookup;

        [UnityEngine.SerializeField]
        List<TRecord> records = new List<TRecord>();

        /// <summary>型付きレコード一覧です。</summary>
        public IReadOnlyList<TRecord> Records => records;

        /// <inheritdoc />
        public override Type RecordType => typeof(TRecord);

        /// <inheritdoc />
        public override MasterIdKind IdKind => CachedIdKind;

        /// <inheritdoc />
        public override int Count => records.Count;

        /// <inheritdoc />
        public override IReadOnlyList<IMasterRecord> AllRecords => records;

        /// <summary>
        /// 正規化キーでレコードを検索します。
        /// </summary>
        /// <param name="key">検索キーです。</param>
        /// <param name="record">見つかったレコードです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryGetByKey(string key, out TRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            EnsureLookup();
            return lookup.TryGetValue(key, out record);
        }

        /// <summary>
        /// 整数 ID でレコードを検索します。ID 種別が Int でない場合は false を返します。
        /// </summary>
        /// <param name="id">整数 ID です。</param>
        /// <param name="record">見つかったレコードです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryGetById(int id, out TRecord record)
        {
            record = null;
            if (IdKind != MasterIdKind.Int)
            {
                return false;
            }

            return TryGetByKey(id.ToString(CultureInfo.InvariantCulture), out record);
        }

        /// <summary>
        /// 文字列 ID でレコードを検索します。ID 種別が String でない場合は false を返します。
        /// </summary>
        /// <param name="id">文字列 ID です。</param>
        /// <param name="record">見つかったレコードです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryGetById(string id, out TRecord record)
        {
            record = null;
            if (IdKind != MasterIdKind.String)
            {
                return false;
            }

            if (id == null)
            {
                id = string.Empty;
            }

            return TryGetByKey(id, out record);
        }

        /// <inheritdoc />
        public override bool TryGetRecordByKey(string key, out IMasterRecord record)
        {
            if (TryGetByKey(key, out TRecord typed))
            {
                record = typed;
                return true;
            }

            record = null;
            return false;
        }

        /// <summary>
        /// レコード一覧を置き換え、検索キャッシュを破棄します。Editor インポーター専用です。
        /// </summary>
        /// <param name="newRecords">新しいレコード列です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="newRecords"/> が null の場合。</exception>
        internal void ReplaceRecords(IEnumerable<TRecord> newRecords)
        {
            if (newRecords == null)
            {
                throw new ArgumentNullException(nameof(newRecords));
            }

            records = new List<TRecord>(newRecords);
            InvalidateLookup();
        }

        /// <summary>検索用辞書キャッシュを破棄します。</summary>
        internal void InvalidateLookup()
        {
            lookup = null;
        }

        /// <summary>Inspector 編集時にキャッシュを破棄します。</summary>
        protected virtual void OnValidate()
        {
            InvalidateLookup();
        }

        void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, TRecord>(StringComparer.Ordinal);
            for (var i = 0; i < records.Count; i++)
            {
                var entry = records[i];
                if (entry == null)
                {
                    continue;
                }

                var key = entry.Key;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (!lookup.ContainsKey(key))
                {
                    lookup[key] = entry;
                }
            }
        }

        static MasterIdKind ResolveIdKind(Type recordType)
        {
            if (typeof(IntIdMasterRecord).IsAssignableFrom(recordType))
            {
                return MasterIdKind.Int;
            }

            if (typeof(StringIdMasterRecord).IsAssignableFrom(recordType))
            {
                return MasterIdKind.String;
            }

            throw new InvalidOperationException(
                "TRecord must inherit IntIdMasterRecord or StringIdMasterRecord. Actual: " + recordType.FullName);
        }
    }
}
