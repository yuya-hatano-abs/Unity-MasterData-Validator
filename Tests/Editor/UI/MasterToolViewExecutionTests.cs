using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 実行ボタンと簡易結果表示のテストです。ウィンドウは開きません。
    /// </summary>
    public sealed class MasterToolViewExecutionTests
    {
        const string JsonPath = "C:/data/items.json";
        const string OtherJsonPath = "C:/data/other.json";
        const string Placeholder = "検証結果は、ここに表示されます。";

        [Test]
        public void Build_InvalidInput_DisablesButtons()
        {
            var harness = new Harness();
            var root = harness.View.Build();
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);
            var save = root.Q<Button>(MasterToolView.SaveButtonName);

            Assert.IsFalse(validate.enabledSelf);
            Assert.IsFalse(save.enabledSelf);
            Assert.AreEqual("入力の問題を解消してください。", validate.tooltip);
            Assert.AreEqual("入力の問題を解消してください。", save.tooltip);
        }

        [Test]
        public void ValidInput_EnablesButtonsWithTooltips()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);
            var save = root.Q<Button>(MasterToolView.SaveButtonName);

            Assert.IsTrue(validate.enabledSelf);
            Assert.IsTrue(save.enabledSelf);
            Assert.AreEqual("JSON を検証します(保存しません)。", validate.tooltip);
            Assert.AreEqual("検証し、エラーがなければ保存します。", save.tooltip);
        }

        [Test]
        public void OnJsonPathChanged_TogglesButtons()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);
            var save = root.Q<Button>(MasterToolView.SaveButtonName);
            Assert.IsTrue(validate.enabledSelf);

            harness.View.OnJsonPathChanged("C:/missing.json");

            Assert.IsFalse(validate.enabledSelf);
            Assert.IsFalse(save.enabledSelf);
            Assert.AreEqual("入力の問題を解消してください。", validate.tooltip);
            Assert.AreEqual("入力の問題を解消してください。", save.tooltip);

            harness.View.OnJsonPathChanged(JsonPath);

            Assert.IsTrue(validate.enabledSelf);
            Assert.IsTrue(save.enabledSelf);
            Assert.AreEqual("JSON を検証します(保存しません)。", validate.tooltip);
            Assert.AreEqual("検証し、エラーがなければ保存します。", save.tooltip);
        }

        [Test]
        public void OnValidateClicked_PassesRequest()
        {
            var harness = new Harness();
            harness.PrepareValid();

            harness.View.OnValidateClicked();

            Assert.AreEqual(1, harness.Runner.Calls);
            var request = harness.Runner.Last;
            Assert.AreEqual(typeof(TestItemMaster), request.MasterType);
            Assert.AreEqual(typeof(TestItemRecord), request.RecordType);
            Assert.AreEqual(JsonPath, request.JsonPath);
            Assert.AreEqual("Assets/MasterData/TestItemMaster.asset", request.AssetPath);
            Assert.AreEqual(PipelineMode.Validate, request.Mode);
        }

        [Test]
        public void OnSaveClicked_UsesValidateAndSave()
        {
            var harness = new Harness();
            harness.PrepareValid();

            harness.View.OnSaveClicked();

            Assert.AreEqual(1, harness.Runner.Calls);
            Assert.AreEqual(PipelineMode.ValidateAndSave, harness.Runner.Last.Mode);
        }

        [Test]
        public void InvalidInput_DoesNotRun()
        {
            var harness = new Harness();
            harness.View.Build();

            harness.View.OnValidateClicked();
            harness.View.OnSaveClicked();

            Assert.AreEqual(0, harness.Runner.Calls);
        }

        [Test]
        public void OnValidateClicked_ShowsFormattedLines()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            var expected = PipelineResultFormatter.Format(harness.Runner.Next);

            harness.View.OnValidateClicked();

            var labels = Labels(root);
            Assert.AreEqual(1 + expected.Count, labels.Count);
            Assert.IsTrue(labels.Any(label => label.text == "検証結果"));
            Assert.IsFalse(labels.Any(label => label.text == Placeholder));
            for (var i = 0; i < expected.Count; i++)
            {
                Assert.IsTrue(labels.Any(label => label.text == expected[i].Text));
            }
        }

        [Test]
        public void Build_ShowsPlaceholderBeforeRun()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();

            Assert.IsTrue(Labels(root).Any(label => label.text == Placeholder));
            Assert.AreEqual(0, harness.Runner.Calls);
        }

        [Test]
        public void InputChanges_ClearResults()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.ExistingFiles.Add(OtherJsonPath);

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.View.OnJsonPathChanged(OtherJsonPath);
            Assert.IsTrue(ShowsPlaceholder(root));
            Assert.IsFalse(ShowsIssue(root));

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.View.OnAssetPathChanged("Assets/Other/Item.asset");
            Assert.IsTrue(ShowsPlaceholder(root));

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.View.OnMasterTypeChanged(typeof(TestQuestMaster).FullName);
            Assert.IsTrue(ShowsPlaceholder(root));

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.Dialogs.JsonToReturn = JsonPath;
            harness.View.OnBrowseJsonClicked();
            Assert.IsTrue(ShowsPlaceholder(root));

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.Dialogs.AssetToReturn = "Assets/MasterData/TestQuestMaster.asset";
            harness.View.OnBrowseAssetClicked();
            Assert.IsTrue(ShowsPlaceholder(root));

            harness.View.OnValidateClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.Dialogs.JsonToReturn = string.Empty;
            harness.View.OnBrowseJsonClicked();
            Assert.IsTrue(ShowsIssue(root));

            harness.Dialogs.AssetToReturn = null;
            harness.View.OnBrowseAssetClicked();
            Assert.IsTrue(ShowsIssue(root));
        }

        [Test]
        public void OnRefreshClicked_KeepsResults()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.View.OnValidateClicked();

            harness.View.OnRefreshClicked();

            Assert.IsTrue(ShowsIssue(root));
            Assert.IsFalse(ShowsPlaceholder(root));
        }

        [Test]
        public void Refresh_KeepsResults()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.View.OnValidateClicked();

            harness.View.Refresh();

            Assert.IsTrue(ShowsIssue(root));
            Assert.IsFalse(ShowsPlaceholder(root));
        }

        [Test]
        public void DuringRun_DisablesButtonsAndIgnoresReentry()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);
            var save = root.Q<Button>(MasterToolView.SaveButtonName);
            harness.Runner.During = () =>
            {
                Assert.IsFalse(validate.enabledSelf);
                Assert.IsFalse(save.enabledSelf);
                Assert.AreEqual("実行中です。", validate.tooltip);
                Assert.AreEqual("実行中です。", save.tooltip);
                var calls = harness.Runner.Calls;
                harness.View.OnValidateClicked();
                harness.View.OnSaveClicked();
                Assert.AreEqual(calls, harness.Runner.Calls);
            };

            harness.View.OnValidateClicked();

            Assert.AreEqual(1, harness.Runner.Calls);
            Assert.IsTrue(validate.enabledSelf);
            Assert.IsTrue(save.enabledSelf);
            Assert.AreEqual("JSON を検証します(保存しません)。", validate.tooltip);
            Assert.AreEqual("検証し、エラーがなければ保存します。", save.tooltip);
        }

        [Test]
        public void Run_Exception_ShowsMessageAndRestoresButtons()
        {
            var harness = new Harness();
            var root = harness.PrepareValid();
            harness.Runner.Throw = new InvalidOperationException("boom");
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);

            Assert.DoesNotThrow(() => harness.View.OnValidateClicked());

            var labels = Labels(root);
            Assert.IsTrue(labels.Any(label => label.text == "実行できませんでした: InvalidOperationException: boom"));
            Assert.IsFalse(labels.Any(label => label.text.StartsWith("[", StringComparison.Ordinal)));
            Assert.IsTrue(validate.enabledSelf);
            Assert.AreEqual("JSON を検証します(保存しません)。", validate.tooltip);
            Assert.AreNotEqual("実行中です。", validate.tooltip);
        }

        [Test]
        public void Constructor_TwoArguments_Works()
        {
            var state = new MasterToolState();
            var catalog = new MasterTypeCatalog(type => type == typeof(TestItemMaster));
            var model = new MasterToolModel(state, catalog, _ => false, _ => null);

            MasterToolView view = null;
            Assert.DoesNotThrow(() => view = new MasterToolView(model, new FakeDialogs()));

            var root = view.Build();
            Assert.IsNotNull(root.Q<Button>(MasterToolView.ValidateButtonName));
        }

        [Test]
        public void Constructor_NullModel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MasterToolView(null, new FakeDialogs(), new FakeRunner()));
        }

        [Test]
        public void Constructor_AcceptsImportRunner()
        {
            var harness = new Harness();
            harness.PrepareValid();

            harness.View.OnValidateClicked();

            Assert.AreEqual(1, harness.Runner.Calls);
            Assert.AreEqual(typeof(TestItemMaster), harness.Runner.Last.MasterType);
        }

        static List<Label> Labels(VisualElement root)
        {
            return root.Q(MasterToolView.ResultsContainerName).Query<Label>().ToList();
        }

        static bool ShowsPlaceholder(VisualElement root)
        {
            return Labels(root).Any(label => label.text == Placeholder);
        }

        static bool ShowsIssue(VisualElement root)
        {
            return Labels(root).Any(label => label.text == "[エラー] one");
        }

        sealed class Harness
        {
            public readonly HashSet<string> ExistingFiles = new HashSet<string>(StringComparer.Ordinal);
            public readonly FakeDialogs Dialogs = new FakeDialogs();
            public readonly FakeRunner Runner = new FakeRunner();
            public readonly MasterToolView View;

            public Harness()
            {
                var catalog = new MasterTypeCatalog(type =>
                    type == typeof(TestItemMaster) || type == typeof(TestQuestMaster));
                var model = new MasterToolModel(
                    new MasterToolState(),
                    catalog,
                    ExistingFiles.Contains,
                    _ => null);
                View = new MasterToolView(model, Dialogs, Runner);
            }

            public VisualElement PrepareValid()
            {
                var root = View.Build();
                View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);
                ExistingFiles.Add(JsonPath);
                View.OnJsonPathChanged(JsonPath);
                return root;
            }
        }

        sealed class FakeDialogs : IFileDialogs
        {
            public string JsonToReturn = string.Empty;
            public string AssetToReturn = string.Empty;

            public string OpenJsonFile(string initialDirectory)
            {
                return JsonToReturn;
            }

            public string SaveAssetFile(string suggestedFileName, string initialAssetFolder)
            {
                return AssetToReturn;
            }
        }

        sealed class FakeRunner : IImportRunner
        {
            public int Calls;
            public PipelineRequest Last;
            public Action During;
            public Exception Throw;
            public PipelineResult Next = Sample();

            public PipelineResult Run(PipelineRequest request)
            {
                Calls++;
                Last = request;
                if (During != null)
                {
                    During();
                }

                if (Throw != null)
                {
                    throw Throw;
                }

                return Next;
            }

            static PipelineResult Sample()
            {
                var issues = new[]
                {
                    new ValidationIssue(
                        ValidationSeverity.Error,
                        ValidationIssueKind.DuplicateId,
                        "test",
                        "one",
                        "TestItemMaster"),
                    new ValidationIssue(
                        ValidationSeverity.Warning,
                        ValidationIssueKind.UnknownField,
                        "test",
                        "two",
                        "TestItemMaster"),
                };
                var table = new MasterTable(
                    "TestItemMaster",
                    typeof(TestItemMaster),
                    typeof(TestItemRecord),
                    MasterIdKind.Int,
                    new IMasterRecord[1],
                    "Assets/A.asset");
                return new PipelineResult(
                    PipelineStatus.ValidationFailed,
                    new ValidationResult(issues),
                    table,
                    false,
                    null,
                    string.Empty);
            }
        }
    }
}
