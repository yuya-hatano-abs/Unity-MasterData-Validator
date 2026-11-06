using System;
using System.Collections.Generic;
using System.Linq;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// バリデーションの結果です。問題一覧は不変です。
    /// </summary>
    public sealed class ValidationResult
    {
        static readonly ValidationResult SharedEmpty = new ValidationResult(Array.Empty<ValidationIssue>());

        readonly ValidationIssue[] issues;

        /// <summary>
        /// 問題 0 件の共有インスタンスです。
        /// </summary>
        public static ValidationResult Empty => SharedEmpty;

        /// <summary>
        /// バリデーション結果を初期化します。
        /// </summary>
        /// <param name="issues">問題一覧です。</param>
        public ValidationResult(IEnumerable<ValidationIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var list = issues as IList<ValidationIssue> ?? issues.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    throw new ArgumentException("issues に null の要素が含まれています。", nameof(issues));
                }
            }

            this.issues = list.ToArray();
            Issues = this.issues;

            var errorCount = 0;
            var warningCount = 0;
            for (var i = 0; i < this.issues.Length; i++)
            {
                if (this.issues[i].Severity == ValidationSeverity.Error)
                {
                    errorCount++;
                }
                else
                {
                    warningCount++;
                }
            }

            ErrorCount = errorCount;
            WarningCount = warningCount;
            HasErrors = errorCount > 0;
            HasWarnings = warningCount > 0;
        }

        /// <summary>問題一覧です。</summary>
        public IReadOnlyList<ValidationIssue> Issues { get; }

        /// <summary>Error の件数です。</summary>
        public int ErrorCount { get; }

        /// <summary>Warning の件数です。</summary>
        public int WarningCount { get; }

        /// <summary>Error が 1 件以上ある場合に true です。</summary>
        public bool HasErrors { get; }

        /// <summary>Warning が 1 件以上ある場合に true です。</summary>
        public bool HasWarnings { get; }

        /// <summary>
        /// Error が 0 件の場合に true です。
        /// Error が 1 件でもあれば false となり、その場合は保存すべきではありません。
        /// Warning のみの場合は true で、保存してよいです。
        /// </summary>
        public bool IsValid => !HasErrors;

        /// <summary>
        /// 複数の結果を、与えられた順に連結します。
        /// </summary>
        /// <param name="results">結果の列です。</param>
        /// <returns>連結された結果です。</returns>
        public static ValidationResult Combine(IEnumerable<ValidationResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var combined = new List<ValidationIssue>();
            foreach (var result in results)
            {
                if (result == null)
                {
                    throw new ArgumentException("results に null の要素が含まれています。", nameof(results));
                }

                combined.AddRange(result.issues);
            }

            if (combined.Count == 0)
            {
                return Empty;
            }

            return new ValidationResult(combined);
        }
    }
}
