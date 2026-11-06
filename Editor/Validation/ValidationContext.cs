using System;
using System.Collections.Generic;
using System.Linq;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// バリデーション実行時の文脈です。
    /// </summary>
    /// <remarks>
    /// 現時点の制限: 1 つのマスタ型につき、テーブルは 1 つだけです。
    /// 同じマスタ型の ScriptableObject アセットが複数ある場合は、
    /// コンストラクタで <see cref="ArgumentException"/> になります。
    /// 将来の拡張で、複数アセットの扱い(参照の解決先、ID の重複の範囲、テーブルの識別)を、別途設計します。
    /// </remarks>
    public sealed class ValidationContext
    {
        readonly MasterTable[] tables;
        readonly MasterTable[] targets;

        /// <summary>
        /// バリデーション文脈を初期化します。
        /// </summary>
        /// <param name="tables">既知の全テーブルです。</param>
        /// <param name="targets">検証対象のテーブルです。null の場合は tables と同じです。</param>
        public ValidationContext(IEnumerable<MasterTable> tables, IEnumerable<MasterTable> targets = null)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            this.tables = CopyAndValidateTables(tables, nameof(tables));
            ValidateUniqueTableKeys(this.tables);

            if (targets == null)
            {
                this.targets = this.tables;
            }
            else
            {
                var targetArray = targets as MasterTable[] ?? targets.ToArray();
                for (var i = 0; i < targetArray.Length; i++)
                {
                    if (targetArray[i] == null)
                    {
                        throw new ArgumentException("targets に null の要素が含まれています。", nameof(targets));
                    }

                    if (!ContainsReference(this.tables, targetArray[i]))
                    {
                        throw new ArgumentException(
                            "targets の要素が tables に含まれていません。",
                            nameof(targets));
                    }
                }

                this.targets = targetArray;
            }

            Tables = this.tables;
            Targets = this.targets;
        }

        /// <summary>既知の全テーブルです。</summary>
        public IReadOnlyList<MasterTable> Tables { get; }

        /// <summary>検証対象のテーブルです。</summary>
        public IReadOnlyList<MasterTable> Targets { get; }

        /// <summary>
        /// マスタ型に一致するテーブルを取得します。
        /// </summary>
        /// <param name="masterType">マスタ型です。</param>
        /// <param name="table">見つかったテーブルです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryGetTable(Type masterType, out MasterTable table)
        {
            table = null;
            if (masterType == null)
            {
                return false;
            }

            for (var i = 0; i < tables.Length; i++)
            {
                if (tables[i].MasterType == masterType)
                {
                    table = tables[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 名前に一致するテーブルを取得します。
        /// </summary>
        /// <param name="name">マスタ名です。</param>
        /// <param name="table">見つかったテーブルです。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryGetTableByName(string name, out MasterTable table)
        {
            table = null;
            if (name == null)
            {
                return false;
            }

            for (var i = 0; i < tables.Length; i++)
            {
                if (string.Equals(tables[i].Name, name, StringComparison.Ordinal))
                {
                    table = tables[i];
                    return true;
                }
            }

            return false;
        }

        static MasterTable[] CopyAndValidateTables(IEnumerable<MasterTable> source, string paramName)
        {
            var list = source as IList<MasterTable> ?? source.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    throw new ArgumentException(paramName + " に null の要素が含まれています。", paramName);
                }
            }

            return list.ToArray();
        }

        static void ValidateUniqueTableKeys(MasterTable[] tableArray)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            var masterTypes = new HashSet<Type>();

            for (var i = 0; i < tableArray.Length; i++)
            {
                var table = tableArray[i];
                if (!names.Add(table.Name))
                {
                    throw new ArgumentException("tables に Name が重複しています: " + table.Name);
                }

                if (table.MasterType != null)
                {
                    if (!masterTypes.Add(table.MasterType))
                    {
                        throw new ArgumentException(
                            "tables に MasterType が重複しています: " + table.MasterType.Name);
                    }
                }
            }
        }

        static bool ContainsReference(MasterTable[] haystack, MasterTable needle)
        {
            for (var i = 0; i < haystack.Length; i++)
            {
                if (ReferenceEquals(haystack[i], needle))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
