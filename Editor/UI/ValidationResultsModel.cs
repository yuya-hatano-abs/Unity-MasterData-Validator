using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 検証結果一覧のフィルター状態です。画面を持ちません。
    /// </summary>
    internal sealed class ValidationResultsModel
    {
        readonly List<ValidationIssue> allIssues = new List<ValidationIssue>();
        readonly List<ValidationIssueKind> availableKinds = new List<ValidationIssueKind>();

        /// <summary>
        /// 空のモデルを初期化します。エラーと警告は、どちらも表示します。
        /// </summary>
        public ValidationResultsModel()
        {
            ShowErrors = true;
            ShowWarnings = true;
        }

        /// <summary>取り込んだ問題です。フィルター前の並びです。</summary>
        public IReadOnlyList<ValidationIssue> AllIssues => allIssues;

        /// <summary>エラーを一覧に含めるかです。</summary>
        public bool ShowErrors { get; set; }

        /// <summary>警告を一覧に含めるかです。</summary>
        public bool ShowWarnings { get; set; }

        /// <summary>種類の絞り込みです。null はすべての種類です。</summary>
        public ValidationIssueKind? KindFilter { get; set; }

        /// <summary>全問題のうちのエラー件数です。フィルターでは変わりません。</summary>
        public int ErrorCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < allIssues.Count; i++)
                {
                    if (allIssues[i].Severity == ValidationSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>全問題のうちの警告件数です。フィルターでは変わりません。</summary>
        public int WarningCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < allIssues.Count; i++)
                {
                    if (allIssues[i].Severity == ValidationSeverity.Warning)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>フィルター後の問題です。元の並びを保ちます。</summary>
        public IReadOnlyList<ValidationIssue> VisibleIssues
        {
            get
            {
                var visible = new List<ValidationIssue>();
                for (var i = 0; i < allIssues.Count; i++)
                {
                    var issue = allIssues[i];
                    if (issue.Severity == ValidationSeverity.Error && !ShowErrors)
                    {
                        continue;
                    }

                    if (issue.Severity == ValidationSeverity.Warning && !ShowWarnings)
                    {
                        continue;
                    }

                    if (KindFilter != null && issue.Kind != KindFilter.Value)
                    {
                        continue;
                    }

                    visible.Add(issue);
                }

                return visible;
            }
        }

        /// <summary>問題に含まれる種類です。列挙値の昇順です。</summary>
        public IReadOnlyList<ValidationIssueKind> AvailableKinds => availableKinds;

        /// <summary>
        /// 問題を差し替えます。null 要素は除きます。呼び出し元のリストを後から変えても、こちらは変わりません。
        /// 種類フィルターが新しい種類に無い場合だけ、null に戻します。重大度のトグルは変えません。
        /// </summary>
        /// <param name="issues">新しい問題です。null は空です。</param>
        public void SetIssues(IEnumerable<ValidationIssue> issues)
        {
            var copy = new List<ValidationIssue>();
            if (issues != null)
            {
                foreach (var issue in issues)
                {
                    if (issue != null)
                    {
                        copy.Add(issue);
                    }
                }
            }

            allIssues.Clear();
            allIssues.AddRange(copy);
            RebuildKinds();
            if (KindFilter != null && !availableKinds.Contains(KindFilter.Value))
            {
                KindFilter = null;
            }
        }

        /// <summary>
        /// 問題を空にします。<see cref="SetIssues"/> に null を渡すのと同じです。
        /// </summary>
        public void Clear()
        {
            SetIssues(null);
        }

        void RebuildKinds()
        {
            availableKinds.Clear();
            for (var i = 0; i < allIssues.Count; i++)
            {
                var kind = allIssues[i].Kind;
                if (!availableKinds.Contains(kind))
                {
                    availableKinds.Add(kind);
                }
            }

            availableKinds.Sort((left, right) => ((int)left).CompareTo((int)right));
        }
    }
}
