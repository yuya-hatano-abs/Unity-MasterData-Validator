using System.Collections.Generic;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationResultsView"/> のテストです。ウィンドウは開かず、アセット選択は偽物です。
    /// </summary>
    public sealed class ValidationResultsViewTests
    {
        [Test]
        public void Build_HasElementNames()
        {
            var root = Create().Build();

            Assert.AreEqual("results-list-panel", root.name);
            Assert.IsNotNull(root.Q("results-filter-bar"));
            Assert.IsNotNull(root.Q<Toggle>("results-filter-errors"));
            Assert.IsNotNull(root.Q<Toggle>("results-filter-warnings"));
            Assert.IsNotNull(root.Q<DropdownField>("results-filter-kind"));
            Assert.IsNotNull(root.Q<Label>("results-visible-count"));
            Assert.IsNotNull(root.Q<ListView>("results-list"));
            Assert.IsNotNull(root.Q<Label>("results-empty-label"));
            Assert.IsNotNull(root.Q<Label>("results-selection-status"));
        }

        [Test]
        public void Build_InitialState()
        {
            var root = Create().Build();

            Assert.AreEqual("エラー (0)", root.Q<Toggle>("results-filter-errors").text);
            Assert.IsTrue(root.Q<Toggle>("results-filter-errors").value);
            Assert.AreEqual("警告 (0)", root.Q<Toggle>("results-filter-warnings").text);
            Assert.IsTrue(root.Q<Toggle>("results-filter-warnings").value);
            Assert.AreEqual("すべて", root.Q<DropdownField>("results-filter-kind").value);
            Assert.AreEqual(1, root.Q<DropdownField>("results-filter-kind").choices.Count);
            Assert.AreEqual("表示中: 0 / 0 件", root.Q<Label>("results-visible-count").text);
            Assert.AreEqual(0, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual("問題はありません。", root.Q<Label>("results-empty-label").text);
            Assert.AreEqual(string.Empty, root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void Build_ConfiguresListHeightAndRows()
        {
            var root = Create().Build();
            var list = root.Q<ListView>("results-list");

            Assert.AreEqual(SelectionType.Single, list.selectionType);
            Assert.AreEqual(CollectionVirtualizationMethod.DynamicHeight, list.virtualizationMethod);
            Assert.AreEqual(48f, list.fixedItemHeight);
            Assert.AreEqual(0f, list.style.flexGrow.value);
            Assert.AreEqual(0f, list.style.flexShrink.value);
            Assert.AreEqual(240f, list.style.height.value.value);
        }

        [Test]
        public void SetIssues_UpdatesCountsChoicesAndHidesEmptyLabel()
        {
            var view = Create();
            var root = view.Build();
            view.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.UnknownField, "e1"),
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e2"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.MissingField, "w1"),
            });

            Assert.AreEqual("エラー (2)", root.Q<Toggle>("results-filter-errors").text);
            Assert.AreEqual("警告 (1)", root.Q<Toggle>("results-filter-warnings").text);
            Assert.AreEqual("表示中: 3 / 3 件", root.Q<Label>("results-visible-count").text);
            Assert.AreEqual(3, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual(DisplayStyle.None, root.Q<Label>("results-empty-label").style.display.value);
            var choices = root.Q<DropdownField>("results-filter-kind").choices;
            Assert.AreEqual(4, choices.Count);
            Assert.AreEqual("すべて", choices[0]);
            Assert.AreEqual("ID の重複", choices[1]);
            Assert.AreEqual("未知のフィールド", choices[2]);
            Assert.AreEqual("フィールドの欠落", choices[3]);
        }

        [Test]
        public void MakeItemAndBindItem_MatchFormatter()
        {
            var view = Create();
            var root = view.Build();
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "duplicate-id",
                "重複しています。",
                "ItemMaster",
                "Assets/ItemMaster.asset",
                3,
                "5",
                "id",
                12);
            view.SetIssues(new[] { issue });
            var list = root.Q<ListView>("results-list");

            var row = list.makeItem();
            list.bindItem(row, 0);

            var primary = row.Q<Label>("issue-primary");
            var detail = row.Q<Label>("issue-detail");
            Assert.AreEqual(IssueRowFormatter.FormatPrimary(issue), primary.text);
            Assert.AreEqual(IssueRowFormatter.FormatDetail(issue), detail.text);
            Assert.AreEqual(IssueRowFormatter.FormatTooltip(issue), row.tooltip);
            Assert.AreEqual(WhiteSpace.Normal, primary.style.whiteSpace.value);
            Assert.AreEqual(WhiteSpace.Normal, detail.style.whiteSpace.value);
            Assert.AreEqual(11f, detail.style.fontSize.value.value);
        }

        [Test]
        public void TogglesAndKindFilter_UpdateVisibleIssues()
        {
            var view = Create();
            var root = view.Build();
            var errorDup = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e");
            var warning = Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w");
            view.SetIssues(new[] { errorDup, warning });

            view.OnShowWarningsChanged(false);

            var list = root.Q<ListView>("results-list");
            Assert.AreEqual(1, list.itemsSource.Count);
            Assert.AreSame(errorDup, list.itemsSource[0]);
            Assert.AreEqual("表示中: 1 / 2 件", root.Q<Label>("results-visible-count").text);
            Assert.AreEqual("エラー (1)", root.Q<Toggle>("results-filter-errors").text);
            Assert.AreEqual("警告 (1)", root.Q<Toggle>("results-filter-warnings").text);

            view.OnKindFilterChanged("ID の重複");

            Assert.AreEqual(ValidationIssueKind.DuplicateId, view.Model.KindFilter);
            Assert.AreEqual(1, list.itemsSource.Count);
            Assert.AreEqual("ID の重複", root.Q<DropdownField>("results-filter-kind").value);
        }

        [Test]
        public void UnknownKindLabel_ClearsKindFilter()
        {
            var view = Create();
            var root = view.Build();
            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e") });
            view.OnKindFilterChanged("ID の重複");

            view.OnKindFilterChanged("ない種類");

            Assert.IsNull(view.Model.KindFilter);
            Assert.AreEqual("すべて", root.Q<DropdownField>("results-filter-kind").value);
            view.OnKindFilterChanged("すべて");
            Assert.IsNull(view.Model.KindFilter);
        }

        [Test]
        public void EmptyLabel_ThreeStates()
        {
            var view = Create();
            var root = view.Build();
            var empty = root.Q<Label>("results-empty-label");
            Assert.AreEqual("問題はありません。", empty.text);
            Assert.AreEqual(DisplayStyle.Flex, empty.style.display.value);

            view.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w"),
            });
            Assert.AreEqual(DisplayStyle.None, empty.style.display.value);

            view.OnShowErrorsChanged(false);
            view.OnShowWarningsChanged(false);
            Assert.AreEqual("フィルター条件に一致する問題はありません。", empty.text);
            Assert.AreEqual(DisplayStyle.Flex, empty.style.display.value);
        }

        [Test]
        public void OnIssueSelected_SelectsAsset()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            var issue = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset");

            view.OnIssueSelected(issue);

            Assert.AreEqual(1, selector.Paths.Count);
            Assert.AreEqual("Assets/A.asset", selector.Paths[0]);
            Assert.AreEqual("選択しました: Assets/A.asset", root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void OnIssueSelected_MissingAsset()
        {
            var selector = new FakeSelector { Result = false };
            var view = Create(selector);
            var root = view.Build();

            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/Missing.asset"));

            Assert.AreEqual(1, selector.Paths.Count);
            Assert.AreEqual(
                "アセットが見つかりません(未保存の可能性があります): Assets/Missing.asset",
                root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void OnIssueSelected_NoAssetPath_DoesNotSelect()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset"));
            var calls = selector.Paths.Count;

            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e"));

            Assert.AreEqual(calls, selector.Paths.Count);
            Assert.AreEqual(
                "この問題には、対応するアセットがありません。",
                root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void OnIssueSelected_EmptyPath_DoesNotSelect()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();

            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", string.Empty));

            Assert.AreEqual(0, selector.Paths.Count);
            Assert.AreEqual(
                "この問題には、対応するアセットがありません。",
                root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void OnIssueSelected_Null_DoesNothing()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset"));
            var text = root.Q<Label>("results-selection-status").text;
            var calls = selector.Paths.Count;

            view.OnIssueSelected(null);

            Assert.AreEqual(calls, selector.Paths.Count);
            Assert.AreEqual(text, root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void SetIssues_ClearsSelectionStatus()
        {
            var view = Create();
            var root = view.Build();
            var issue = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset");
            view.OnIssueSelected(issue);

            view.SetIssues(new[] { issue });

            Assert.AreEqual(string.Empty, root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void FilterChange_ClearsSelectionStatus()
        {
            var view = Create();
            var root = view.Build();
            var issue = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset");
            view.SetIssues(new[] { issue });
            var status = root.Q<Label>("results-selection-status");

            view.OnIssueSelected(issue);
            view.OnShowErrorsChanged(false);
            Assert.AreEqual(string.Empty, status.text);

            view.OnIssueSelected(issue);
            view.OnShowWarningsChanged(true);
            Assert.AreEqual(string.Empty, status.text);

            view.OnIssueSelected(issue);
            view.OnKindFilterChanged("すべて");
            Assert.AreEqual(string.Empty, status.text);
        }

        [Test]
        public void Clear_EmptiesListAndKeepsToggles()
        {
            var view = Create();
            var root = view.Build();
            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e") });
            view.OnShowErrorsChanged(false);
            view.OnKindFilterChanged("ID の重複");

            view.Clear();

            Assert.AreEqual(0, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual("問題はありません。", root.Q<Label>("results-empty-label").text);
            Assert.AreEqual(string.Empty, root.Q<Label>("results-selection-status").text);
            Assert.IsFalse(view.Model.ShowErrors);
            Assert.IsTrue(view.Model.ShowWarnings);
            Assert.IsNull(view.Model.KindFilter);
            Assert.IsFalse(root.Q<Toggle>("results-filter-errors").value);
        }

        [Test]
        public void SetIssues_KeepsSeverityTogglesAndKindFilter()
        {
            var view = Create();
            var root = view.Build();
            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e1") });
            view.OnShowErrorsChanged(false);
            view.OnKindFilterChanged("ID の重複");

            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e2") });

            Assert.IsFalse(view.Model.ShowErrors);
            Assert.AreEqual(ValidationIssueKind.DuplicateId, view.Model.KindFilter);
            Assert.IsFalse(root.Q<Toggle>("results-filter-errors").value);
            Assert.AreEqual("ID の重複", root.Q<DropdownField>("results-filter-kind").value);
            Assert.AreEqual(0, root.Q<ListView>("results-list").itemsSource.Count);
        }

        [Test]
        public void SetIssues_ResetsKindFilterWhenKindIsGone()
        {
            var view = Create();
            var root = view.Build();
            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e") });
            view.OnShowErrorsChanged(false);
            view.OnKindFilterChanged("ID の重複");

            view.SetIssues(new[] { Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w") });

            Assert.IsFalse(view.Model.ShowErrors);
            Assert.IsNull(view.Model.KindFilter);
            Assert.AreEqual("すべて", root.Q<DropdownField>("results-filter-kind").value);
            Assert.AreEqual(1, root.Q<ListView>("results-list").itemsSource.Count);
        }

        [Test]
        public void SetIssues_TwoThousandIssues_DoesNotThrow()
        {
            var view = Create();
            var root = view.Build();
            var issues = new ValidationIssue[2000];
            for (var i = 0; i < issues.Length; i++)
            {
                issues[i] = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "m" + i);
            }

            Assert.DoesNotThrow(() => view.SetIssues(issues));

            Assert.AreEqual(2000, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual("表示中: 2000 / 2000 件", root.Q<Label>("results-visible-count").text);
        }

        [Test]
        public void Build_Twice_SharesModel()
        {
            var view = Create();
            var first = view.Build();
            view.SetIssues(new[] { Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e") });
            view.OnShowErrorsChanged(false);
            view.OnKindFilterChanged("ID の重複");
            var model = view.Model;

            var second = view.Build();

            Assert.AreNotSame(first, second);
            Assert.AreSame(model, view.Model);
            Assert.IsFalse(second.Q<Toggle>("results-filter-errors").value);
            Assert.AreEqual("ID の重複", second.Q<DropdownField>("results-filter-kind").value);
            Assert.AreEqual(0, second.Q<ListView>("results-list").itemsSource.Count);
        }

        [Test]
        public void Refresh_DoesNotChangeModelOrStatus()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            var issue = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset");
            view.SetIssues(new[] { issue });
            view.OnIssueSelected(issue);
            var status = root.Q<Label>("results-selection-status").text;
            view.Model.ShowWarnings = false;

            view.Refresh();

            Assert.IsFalse(view.Model.ShowWarnings);
            Assert.IsTrue(view.Model.ShowErrors);
            Assert.IsFalse(root.Q<Toggle>("results-filter-warnings").value);
            Assert.AreEqual(status, root.Q<Label>("results-selection-status").text);
            Assert.AreEqual(1, selector.Paths.Count);
        }

        [Test]
        public void SelectionChanged_PassesFirstIssue()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            var first = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "first", "Assets/First.asset");
            var second = Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "second", "Assets/Second.asset");

            view.OnSelectionChanged(new object[] { "skip", first, second });

            Assert.AreEqual(1, selector.Paths.Count);
            Assert.AreEqual("Assets/First.asset", selector.Paths[0]);
            Assert.AreEqual("選択しました: Assets/First.asset", root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void SelectionChanged_EmptyOrNonIssue_KeepsStatus()
        {
            var selector = new FakeSelector();
            var view = Create(selector);
            var root = view.Build();
            view.OnIssueSelected(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e", "Assets/A.asset"));
            var status = root.Q<Label>("results-selection-status").text;
            var calls = selector.Paths.Count;

            view.OnSelectionChanged(new object[0]);
            view.OnSelectionChanged(null);
            view.OnSelectionChanged(new object[] { "not-an-issue", 12 });

            Assert.AreEqual(calls, selector.Paths.Count);
            Assert.AreEqual(status, root.Q<Label>("results-selection-status").text);
        }

        static ValidationResultsView Create(FakeSelector selector = null)
        {
            return new ValidationResultsView(null, selector ?? new FakeSelector());
        }

        static ValidationIssue Issue(
            ValidationSeverity severity,
            ValidationIssueKind kind,
            string message,
            string assetPath = null)
        {
            return new ValidationIssue(severity, kind, "test", message, "ItemMaster", assetPath);
        }

        sealed class FakeSelector : IAssetSelector
        {
            public readonly List<string> Paths = new List<string>();
            public bool Result = true;

            public bool TrySelect(string assetPath)
            {
                Paths.Add(assetPath);
                return Result;
            }
        }
    }
}
