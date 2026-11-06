using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using UnityEditor;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// 検証対象のマスタテーブルのスナップショットです。レコードのリストは作成時にコピーされますが、
    /// 各レコードのインスタンスは元と共有されます。検証器は、レコードを変更してはなりません。
    /// </summary>
    /// <remarks>
    /// 現時点の制限: 1 つのマスタ型につき、テーブルは 1 つだけです。
    /// 同じマスタ型の ScriptableObject アセットが複数ある場合は、
    /// <see cref="ValidationContext"/> の作成時に <see cref="ArgumentException"/> になります。
    /// 将来の拡張で、複数アセットの扱い(参照の解決先、ID の重複の範囲、テーブルの識別)を、別途設計します。
    /// </remarks>
    public sealed class MasterTable
    {
        readonly IMasterRecord[] records;

        /// <summary>
        /// マスタテーブルのスナップショットを初期化します。
        /// </summary>
        public MasterTable(
            string name,
            Type masterType,
            Type recordType,
            MasterIdKind idKind,
            IEnumerable<IMasterRecord> records,
            string assetPath)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name が空です。", nameof(name));
            }

            if (recordType == null)
            {
                throw new ArgumentNullException(nameof(recordType));
            }

            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            Name = name;
            MasterType = masterType;
            RecordType = recordType;
            IdKind = idKind;
            AssetPath = assetPath;
            this.records = records.ToArray();
            Records = this.records;
        }

        /// <summary>マスタ型の名前です(例: ItemMaster)。</summary>
        public string Name { get; }

        /// <summary>マスタ型です。不明な場合は null です。</summary>
        public Type MasterType { get; }

        /// <summary>レコード型です。</summary>
        public Type RecordType { get; }

        /// <summary>ID の種類です。</summary>
        public MasterIdKind IdKind { get; }

        /// <summary>アセットパスです。保存前または不明な場合は null です。</summary>
        public string AssetPath { get; }

        /// <summary>レコード一覧です。</summary>
        public IReadOnlyList<IMasterRecord> Records { get; }

        /// <summary>
        /// 保存済みまたはメモリ上のマスタアセットからスナップショットを作成します。
        /// </summary>
        /// <param name="asset">マスタアセットです。</param>
        /// <returns>スナップショットです。</returns>
        public static MasterTable FromAsset(MasterDataBase asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                path = null;
            }

            var recordsCopy = new IMasterRecord[asset.Count];
            var all = asset.AllRecords;
            for (var i = 0; i < all.Count; i++)
            {
                recordsCopy[i] = all[i];
            }

            return new MasterTable(
                asset.GetType().Name,
                asset.GetType(),
                asset.RecordType,
                asset.IdKind,
                recordsCopy,
                path);
        }

        /// <summary>
        /// インポート結果からスナップショットを作成します。
        /// </summary>
        public static MasterTable FromImport<TMaster, TRecord>(
            ImportResult<TRecord> importResult,
            string assetPath)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            if (importResult == null)
            {
                throw new ArgumentNullException(nameof(importResult));
            }

            var idKind = ResolveIdKind(typeof(TRecord));
            var recordsCopy = new IMasterRecord[importResult.Records.Count];
            for (var i = 0; i < importResult.Records.Count; i++)
            {
                recordsCopy[i] = importResult.Records[i];
            }

            return new MasterTable(
                typeof(TMaster).Name,
                typeof(TMaster),
                typeof(TRecord),
                idKind,
                recordsCopy,
                assetPath);
        }

        internal static MasterIdKind ResolveIdKind(Type recordType)
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
