using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 検証結果一覧をウィンドウへ載せるテストです。ウィンドウは開かず、実行と選択は偽物です。
    /// </summary>
    public sealed class MasterToolViewResultsTests
    {
        const string JsonPath = "C:/data/items.json";
        const string OtherJsonPath = "C:/data/other.json";
        const string Placeholder = "検証結果は、ここに表示されます。";

        [Test]
        public void BeforeRun_ShowsHeadingAndPlaceholderOnly()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            var container = root.Q(MasterToolView.ResultsContainerName);

            Assert.AreEqual(2, container.childCount);
            Assert.AreEqual("検証結果", ((Label)container[0]).text);
            Assert.AreEqual(Placeholder, ((Label)container[1]).text);
            Assert.IsNull(root.Q(MasterToolView.ResultsHeaderName));
            Assert.IsNull(root.Q(MasterToolView.ResultsListPanelName));
        }

        [Test]
        public void ValidationFailed_ShowsHeaderAndAllIssues()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = Failed("one", "two", "three");
            var headerLines = PipelineResultFormatter.FormatHeader(harness.Runner.Next);

            harness.View.OnValidateClicked();

            var container = root.Q(MasterToolView.ResultsContainerName);
            Assert.AreEqual(3, container.childCount);
            Assert.AreEqual("検証結果", ((Label)container[0]).text);
            Assert.AreEqual(MasterToolView.ResultsHeaderName, container[1].name);
            Assert.AreEqual(MasterToolView.ResultsListPanelName, container[2].name);
            Assert.AreEqual(1, DirectLabelCount(container));
            var headerLabels = container.Q(MasterToolView.ResultsHeaderName).Query<Label>().ToList();
            Assert.AreEqual(headerLines.Count, headerLabels.Count);
            for (var i = 0; i < headerLines.Count; i++)
            {
                Assert.AreEqual(headerLines[i].Text, headerLabels[i].text);
            }

            Assert.AreEqual(3, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.IsFalse(ContainsText(container, Placeholder));
        }

        [Test]
        public void Saved_WithNoIssues_ShowsEmptyMessage()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = Saved();
            var headerLines = PipelineResultFormatter.FormatHeader(harness.Runner.Next);

            harness.View.OnValidateClicked();

            var headerLabels = root.Q(MasterToolView.ResultsHeaderName).Query<Label>().ToList();
            Assert.AreEqual(headerLines.Count, headerLabels.Count);
            Assert.AreEqual("検証しました: エラー 0 件、警告 0 件。(レコード 1 件)", headerLabels[0].text);
            Assert.AreEqual(
                "保存しました(新規作成): Assets/MasterData/TestItemMaster.asset(1 件)",
                headerLabels[1].text);
            Assert.AreEqual(0, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual("問題はありません。", root.Q<Label>("results-empty-label").text);
            Assert.IsFalse(ContainsText(root.Q(MasterToolView.ResultsContainerName), Placeholder));
        }

        [Test]
        public void InvalidRequest_ShowsHeaderWithoutList()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = new PipelineResult(
                PipelineStatus.InvalidRequest,
                ValidationResult.Empty,
                null,
                false,
                null,
                "理由です");

            harness.View.OnValidateClicked();

            AssertHeaderOnly(root, "実行できませんでした: 理由です");
        }

        [Test]
        public void UnexpectedError_ShowsHeaderWithoutList()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = new PipelineResult(
                PipelineStatus.UnexpectedError,
                ValidationResult.Empty,
                null,
                false,
                null,
                "InvalidOperationException: boom");

            harness.View.OnValidateClicked();

            AssertHeaderOnly(root, "実行できませんでした: InvalidOperationException: boom");
        }

        [Test]
        public void RunnerException_ShowsMessageWithoutListAndEnablesButtons()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Throw = new InvalidOperationException("boom");
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);

            harness.View.OnValidateClicked();

            AssertHeaderOnly(root, "実行できませんでした: InvalidOperationException: boom");
            Assert.IsTrue(validate.enabledSelf);
            Assert.AreEqual("JSON を検証します(保存しません)。", validate.tooltip);
        }

        [Test]
        public void InputChange_RestoresPlaceholderAndKeepsErrorToggle()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = Failed("one", "two");
            harness.View.OnValidateClicked();
            harness.View.ResultsView.OnShowErrorsChanged(false);

            harness.View.OnJsonPathChanged(OtherJsonPath);

            var container = root.Q(MasterToolView.ResultsContainerName);
            Assert.IsTrue(ContainsText(container, Placeholder));
            Assert.IsNull(root.Q(MasterToolView.ResultsListPanelName));
            Assert.IsFalse(harness.View.ResultsView.Model.ShowErrors);

            harness.View.OnValidateClicked();

            Assert.IsFalse(harness.View.ResultsView.Model.ShowErrors);
            Assert.IsFalse(root.Q<Toggle>("results-filter-errors").value);
            Assert.AreEqual("エラー (2)", root.Q<Toggle>("results-filter-errors").text);
            Assert.AreEqual(0, root.Q<ListView>("results-list").itemsSource.Count);
            Assert.AreEqual(
                "フィルター条件に一致する問題はありません。",
                root.Q<Label>("results-empty-label").text);
        }

        [Test]
        public void SecondRun_ReplacesIssueCount()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Next = Failed("one", "two", "three");
            harness.View.OnValidateClicked();
            Assert.AreEqual(3, root.Q<ListView>("results-list").itemsSource.Count);

            harness.Runner.Next = Failed("only");
            harness.View.OnValidateClicked();

            var list = root.Q<ListView>("results-list");
            Assert.AreEqual(1, list.itemsSource.Count);
            Assert.AreEqual("only", ((ValidationIssue)list.itemsSource[0]).Message);
        }

        [Test]
        public void OnIssueSelected_UsesInjectedSelector()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.View.OnValidateClicked();
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "test",
                "picked",
                "TestItemMaster",
                "Assets/Picked.asset");

            harness.View.ResultsView.OnIssueSelected(issue);

            Assert.AreEqual(1, harness.Selector.Paths.Count);
            Assert.AreEqual("Assets/Picked.asset", harness.Selector.Paths[0]);
            Assert.AreEqual(
                "選択しました: Assets/Picked.asset",
                root.Q<Label>("results-selection-status").text);
        }

        [Test]
        public void Constructor_OneArgument_Builds()
        {
            var model = NewModel();

            var view = new MasterToolView(model);
            var root = view.Build();

            Assert.IsNotNull(root.Q<Button>(MasterToolView.ValidateButtonName));
            Assert.IsNotNull(view.ResultsView);
        }

        [Test]
        public void Constructor_FourArguments_Builds()
        {
            var model = NewModel();
            var selector = new FakeSelector();

            var view = new MasterToolView(model, new FakeDialogs(), new FakeRunner(), selector);
            var root = view.Build();
            view.ResultsView.OnIssueSelected(new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "test",
                "x",
                assetPath: "Assets/FromCtor.asset"));

            Assert.IsNotNull(root.Q<Button>(MasterToolView.SaveButtonName));
            Assert.AreEqual(1, selector.Paths.Count);
            Assert.AreEqual("Assets/FromCtor.asset", selector.Paths[0]);
        }

        static void AssertHeaderOnly(VisualElement root, string text)
        {
            var container = root.Q(MasterToolView.ResultsContainerName);
            Assert.AreEqual(2, container.childCount);
            Assert.AreEqual(MasterToolView.ResultsHeaderName, container[1].name);
            Assert.AreEqual(text, container.Q(MasterToolView.ResultsHeaderName).Q<Label>().text);
            Assert.IsNull(root.Q(MasterToolView.ResultsListPanelName));
            Assert.IsFalse(ContainsText(container, Placeholder));
        }

        static int DirectLabelCount(VisualElement container)
        {
            var count = 0;
            for (var i = 0; i < container.childCount; i++)
            {
                if (container[i] is Label)
                {
                    count++;
                }
            }

            return count;
        }

        static bool ContainsText(VisualElement root, string text)
        {
            var labels = root.Query<Label>().ToList();
            for (var i = 0; i < labels.Count; i++)
            {
                if (labels[i].text == text)
                {
                    return true;
                }
            }

            return false;
        }

        static MasterToolModel NewModel()
        {
            var catalog = new MasterTypeCatalog(type => type == typeof(TestItemMaster));
            return new MasterToolModel(new MasterToolState(), catalog, _ => false, _ => null);
        }

        static PipelineResult Failed(params string[] messages)
        {
            var issues = new ValidationIssue[messages.Length];
            for (var i = 0; i < messages.Length; i++)
            {
                issues[i] = new ValidationIssue(
                    ValidationSeverity.Error,
                    ValidationIssueKind.DuplicateId,
                    "test",
                    messages[i],
                    "TestItemMaster");
            }

            return new PipelineResult(
                PipelineStatus.ValidationFailed,
                new ValidationResult(issues),
                Table(),
                false,
                null,
                string.Empty);
        }

        static PipelineResult Saved()
        {
            return new PipelineResult(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(),
                true,
                new PipelineSaveInfo(
                    SaveOutcome.Created,
                    "Assets/MasterData/TestItemMaster.asset",
                    -1,
                    1,
                    null),
                string.Empty);
        }

        static MasterTable Table()
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[1],
                "Assets/MasterData/TestItemMaster.asset");
        }

        sealed class Harness
        {
            public readonly FakeSelector Selector = new FakeSelector();
            public readonly FakeRunner Runner = new FakeRunner();
            public readonly MasterToolView View;

            public Harness()
            {
                var catalog = new MasterTypeCatalog(type =>
                    type == typeof(TestItemMaster) || type == typeof(TestQuestMaster));
                var files = new HashSet<string>(StringComparer.Ordinal) { JsonPath, OtherJsonPath };
                var model = new MasterToolModel(new MasterToolState(), catalog, files.Contains, _ => null);
                View = new MasterToolView(model, new FakeDialogs(), Runner, Selector);
            }

            public VisualElement PrepareValid()
            {
                var root = View.Build();
                View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);
                View.OnJsonPathChanged(JsonPath);
                return root;
            }
        }

        sealed class FakeDialogs : IFileDialogs
        {
            public string OpenJsonFile(string initialDirectory)
            {
                return string.Empty;
            }

            public string SaveAssetFile(string suggestedFileName, string initialAssetFolder)
            {
                return string.Empty;
            }
        }

        sealed class FakeRunner : IImportRunner
        {
            public Exception Throw;
            public PipelineResult Next = Failed("sample");

            public PipelineResult Run(PipelineRequest request)
            {
                if (Throw != null)
                {
                    throw Throw;
                }

                return Next;
            }
        }

        sealed class FakeSelector : IAssetSelector
        {
            public readonly List<string> Paths = new List<string>();

            public bool TrySelect(string assetPath)
            {
                Paths.Add(assetPath);
                return true;
            }
        }
    }
}
