using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// プロジェクトから集めた参照先テーブルと、それに伴う警告です。
    /// </summary>
    internal sealed class ProjectTables
    {
        /// <summary>空の一覧です。</summary>
        public static readonly ProjectTables Empty = new ProjectTables(
            new MasterTable[0],
            new ValidationIssue[0]);

        /// <summary>
        /// 一覧を初期化します。
        /// </summary>
        /// <param name="tables">参照先に使えるテーブルです。</param>
        /// <param name="warnings">集め方に関する警告です。</param>
        public ProjectTables(IReadOnlyList<MasterTable> tables, IReadOnlyList<ValidationIssue> warnings)
        {
            Tables = tables ?? new MasterTable[0];
            Warnings = warnings ?? new ValidationIssue[0];
        }

        /// <summary>参照先に使えるテーブルです。</summary>
        public IReadOnlyList<MasterTable> Tables { get; }

        /// <summary>集め方に関する警告です。</summary>
        public IReadOnlyList<ValidationIssue> Warnings { get; }
    }
}
