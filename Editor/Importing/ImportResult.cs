using System.Collections.Generic;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// JSON インポートの結果です。
    /// </summary>
    /// <typeparam name="TRecord">レコード型です。</typeparam>
    public sealed class ImportResult<TRecord>
        where TRecord : MasterRecordBase
    {
        internal ImportResult(
            IReadOnlyList<TRecord> records,
            IReadOnlyList<ImportIssue> issues,
            bool succeeded)
        {
            Records = records;
            Issues = issues;
            Succeeded = succeeded;
        }

        /// <summary>取り込まれたレコード一覧です。</summary>
        public IReadOnlyList<TRecord> Records { get; }

        /// <summary>検出された問題一覧です。</summary>
        public IReadOnlyList<ImportIssue> Issues { get; }

        /// <summary>
        /// JSON をパースでき、records 配列が見つかった場合に true です。
        /// フィールド単位の問題があっても true になることがあります。
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>重大度 Error の問題が 1 件以上ある場合に true です。</summary>
        public bool HasErrors
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == ImportSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>重大度 Warning の問題が 1 件以上ある場合に true です。</summary>
        public bool HasWarnings
        {
            get
            {
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i].Severity == ImportSeverity.Warning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
