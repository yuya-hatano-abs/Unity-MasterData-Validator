using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterToolView"/> のテストです。ウィンドウは開きません。
    /// </summary>
    public sealed class MasterToolViewTests
    {
        [Test]
        public void ElementNames_MatchSpecification()
        {
            Assert.AreEqual("master-type-field", MasterToolView.MasterTypeFieldName);
            Assert.AreEqual("master-type-refresh-button", MasterToolView.MasterTypeRefreshButtonName);
            Assert.AreEqual("master-type-empty-help", MasterToolView.MasterTypeEmptyHelpName);
            Assert.AreEqual("json-path-field", MasterToolView.JsonPathFieldName);
            Assert.AreEqual("json-browse-button", MasterToolView.JsonBrowseButtonName);
            Assert.AreEqual("asset-path-field", MasterToolView.AssetPathFieldName);
            Assert.AreEqual("asset-browse-button", MasterToolView.AssetBrowseButtonName);
            Assert.AreEqual("input-problems", MasterToolView.InputProblemsName);
            Assert.AreEqual("validate-button", MasterToolView.ValidateButtonName);
            Assert.AreEqual("save-button", MasterToolView.SaveButtonName);
            Assert.AreEqual("results-container", MasterToolView.ResultsContainerName);
        }

        [Test]
        public void Build_CreatesNamedElements()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();

            Assert.IsNotNull(root.Q<DropdownField>(MasterToolView.MasterTypeFieldName));
            Assert.IsNotNull(root.Q<Button>(MasterToolView.MasterTypeRefreshButtonName));
            Assert.IsNotNull(root.Q<HelpBox>(MasterToolView.MasterTypeEmptyHelpName));
            Assert.IsNotNull(root.Q<TextField>(MasterToolView.JsonPathFieldName));
            Assert.IsNotNull(root.Q<Button>(MasterToolView.JsonBrowseButtonName));
            Assert.IsNotNull(root.Q<TextField>(MasterToolView.AssetPathFieldName));
            Assert.IsNotNull(root.Q<Button>(MasterToolView.AssetBrowseButtonName));
            Assert.IsNotNull(root.Q(MasterToolView.InputProblemsName));
            Assert.IsNotNull(root.Q<Button>(MasterToolView.ValidateButtonName));
            Assert.IsNotNull(root.Q<Button>(MasterToolView.SaveButtonName));
            Assert.IsNotNull(root.Q(MasterToolView.ResultsContainerName));
        }

        [Test]
        public void Build_DisablesExecutionButtons()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            var validate = root.Q<Button>(MasterToolView.ValidateButtonName);
            var save = root.Q<Button>(MasterToolView.SaveButtonName);

            Assert.IsFalse(validate.enabledSelf);
            Assert.IsFalse(save.enabledSelf);
            Assert.AreEqual("入力の問題を解消してください。", validate.tooltip);
            Assert.AreEqual("入力の問題を解消してください。", save.tooltip);
            Assert.AreEqual("検証", validate.text);
            Assert.AreEqual("検証して保存", save.text);
        }

        [Test]
        public void Build_ShowsResultsPlaceholder()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            var results = root.Q(MasterToolView.ResultsContainerName);
            var labels = results.Query<Label>().ToList();

            Assert.IsTrue(labels.Any(label => label.text == "検証結果"));
            Assert.IsTrue(labels.Any(label => label.text == "検証結果は、ここに表示されます。"));
        }

        [Test]
        public void Build_ReflectsInitialState()
        {
            var harness = new ViewHarness();
            harness.State.JsonPath = "C:/data/items.json";
            harness.State.AssetPath = "Assets/Custom/Item.asset";
            harness.State.SelectedTypeName = typeof(TestItemMaster).FullName;

            var root = harness.View.Build();

            Assert.AreEqual(
                typeof(TestItemMaster).FullName,
                root.Q<DropdownField>(MasterToolView.MasterTypeFieldName).value);
            Assert.AreEqual("C:/data/items.json", root.Q<TextField>(MasterToolView.JsonPathFieldName).value);
            Assert.AreEqual("Assets/Custom/Item.asset", root.Q<TextField>(MasterToolView.AssetPathFieldName).value);
        }

        [Test]
        public void Build_DropdownChoicesMatchCatalog()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            var dropdown = root.Q<DropdownField>(MasterToolView.MasterTypeFieldName);
            var expected = harness.Catalog.Types.Select(info => info.DisplayName).ToList();

            CollectionAssert.AreEqual(expected, dropdown.choices);
            Assert.AreEqual(2, dropdown.choices.Count);
        }

        [Test]
        public void Build_HidesEmptyHelpWhenTypesExist()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            var help = root.Q<HelpBox>(MasterToolView.MasterTypeEmptyHelpName);

            Assert.AreEqual(DisplayStyle.None, help.style.display.value);
        }

        [Test]
        public void Build_ShowsEmptyHelpWhenCatalogIsEmpty()
        {
            var catalog = new MasterTypeCatalog(_ => false);
            var model = new MasterToolModel(new MasterToolState(), catalog, _ => false, _ => null);
            var view = new MasterToolView(model, new FakeFileDialogs());

            var root = view.Build();
            var help = root.Q<HelpBox>(MasterToolView.MasterTypeEmptyHelpName);

            Assert.AreNotEqual(DisplayStyle.None, help.style.display.value);
            StringAssert.Contains("見つかりません", help.text);
        }

        [Test]
        public void Build_ShowsInputProblems()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            var boxes = root.Q(MasterToolView.InputProblemsName).Query<HelpBox>().ToList();

            Assert.AreEqual(3, boxes.Count);
            Assert.AreEqual(HelpBoxMessageType.Error, boxes[0].messageType);
            Assert.AreEqual(HelpBoxMessageType.Error, boxes[1].messageType);
            Assert.AreEqual(HelpBoxMessageType.Error, boxes[2].messageType);
        }

        [Test]
        public void OnMasterTypeChanged_UpdatesModelAndFields()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();

            harness.View.OnMasterTypeChanged(typeof(TestQuestMaster).FullName);

            Assert.AreEqual(typeof(TestQuestMaster).FullName, harness.State.SelectedTypeName);
            Assert.AreEqual("Assets/MasterData/TestQuestMaster.asset", harness.State.AssetPath);
            Assert.AreEqual(
                "Assets/MasterData/TestQuestMaster.asset",
                root.Q<TextField>(MasterToolView.AssetPathFieldName).value);
        }

        [Test]
        public void OnJsonPathChanged_UpdatesProblems()
        {
            var harness = new ViewHarness();
            var root = harness.View.Build();
            harness.View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);
            harness.ExistingFiles.Add("C:/data/items.json");

            harness.View.OnJsonPathChanged("C:/data/items.json");

            var boxes = root.Q(MasterToolView.InputProblemsName).Query<HelpBox>().ToList();
            Assert.AreEqual(0, boxes.Count);
        }

        [Test]
        public void OnAssetPathChanged_ShowsCompatibleInfo()
        {
            var harness = new ViewHarness();
            harness.Lookup = _ => typeof(TestItemMaster);
            var root = harness.View.Build();
            harness.View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);
            harness.ExistingFiles.Add("C:/data/items.json");
            harness.View.OnJsonPathChanged("C:/data/items.json");

            harness.View.OnAssetPathChanged("Assets/MasterData/TestItemMaster.asset");

            var boxes = root.Q(MasterToolView.InputProblemsName).Query<HelpBox>().ToList();
            Assert.AreEqual(1, boxes.Count);
            Assert.AreEqual(HelpBoxMessageType.Info, boxes[0].messageType);
            Assert.AreEqual("既存のアセットを更新します。", boxes[0].text);
        }

        [Test]
        public void OnRefreshClicked_RebuildsChoices()
        {
            var includeQuest = false;
            var catalog = new MasterTypeCatalog(type =>
                type == typeof(TestItemMaster) || (includeQuest && type == typeof(TestQuestMaster)));
            var model = new MasterToolModel(new MasterToolState(), catalog, _ => false, _ => null);
            var view = new MasterToolView(model, new FakeFileDialogs());
            var root = view.Build();
            var dropdown = root.Q<DropdownField>(MasterToolView.MasterTypeFieldName);
            Assert.AreEqual(1, dropdown.choices.Count);

            includeQuest = true;
            view.OnRefreshClicked();

            Assert.AreEqual(2, dropdown.choices.Count);
        }

        [Test]
        public void OnBrowseJsonClicked_SetsPathAndLastDirectory()
        {
            var harness = new ViewHarness();
            harness.View.Build();
            var picked = "C:/data/items.json";
            harness.Dialogs.JsonToReturn = picked;

            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual(picked, harness.State.JsonPath);
            Assert.AreEqual(Path.GetDirectoryName(picked), harness.State.LastJsonDirectory);
        }

        [Test]
        public void OnBrowseJsonClicked_Cancel_ChangesNothing()
        {
            var harness = new ViewHarness();
            harness.State.JsonPath = "C:/keep/old.json";
            harness.State.LastJsonDirectory = Path.GetDirectoryName("C:/keep/old.json");
            harness.View.Build();
            harness.Dialogs.JsonToReturn = string.Empty;

            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual("C:/keep/old.json", harness.State.JsonPath);
            Assert.AreEqual(Path.GetDirectoryName("C:/keep/old.json"), harness.State.LastJsonDirectory);
        }

        [Test]
        public void OnBrowseJsonClicked_InitialDirectory_PrefersLastDirectory()
        {
            var harness = new ViewHarness();
            harness.State.LastJsonDirectory = Path.GetDirectoryName("D:/keep/file.json");
            harness.State.JsonPath = "C:/data/items.json";
            harness.View.Build();

            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual(harness.State.LastJsonDirectory, harness.Dialogs.ReceivedJsonDirectory);
        }

        [Test]
        public void OnBrowseJsonClicked_InitialDirectory_UsesJsonPathDirectory()
        {
            var harness = new ViewHarness();
            harness.State.JsonPath = "C:/data/items.json";
            harness.View.Build();

            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual(Path.GetDirectoryName(harness.State.JsonPath), harness.Dialogs.ReceivedJsonDirectory);
        }

        [Test]
        public void OnBrowseJsonClicked_InitialDirectory_UsesProjectRoot()
        {
            var harness = new ViewHarness();
            harness.View.Build();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;

            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual(projectRoot, harness.Dialogs.ReceivedJsonDirectory);
        }

        [Test]
        public void OnBrowseAssetClicked_SuggestedNameFollowsSelection()
        {
            var harness = new ViewHarness();
            harness.View.Build();
            harness.View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);
            harness.Dialogs.AssetToReturn = "Assets/MasterData/TestItemMaster.asset";

            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual("TestItemMaster.asset", harness.Dialogs.ReceivedSuggestedName);
            Assert.AreEqual("Assets/MasterData/TestItemMaster.asset", harness.State.AssetPath);
        }

        [Test]
        public void OnBrowseAssetClicked_Unselected_UsesMasterAssetName()
        {
            var harness = new ViewHarness();
            harness.View.Build();

            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual("Master.asset", harness.Dialogs.ReceivedSuggestedName);
        }

        [Test]
        public void OnBrowseAssetClicked_InitialFolder_UsesAssetDirectory()
        {
            var harness = new ViewHarness();
            harness.State.AssetPath = "Assets/MasterData/Item.asset";
            harness.View.Build();
            var expected = Path.GetDirectoryName(harness.State.AssetPath).Replace('\\', '/');

            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual(expected, harness.Dialogs.ReceivedAssetFolder);
        }

        [Test]
        public void OnBrowseAssetClicked_InitialFolder_FallsBackToAssets()
        {
            var harness = new ViewHarness();
            harness.State.AssetPath = Path.GetFullPath("outside.asset");
            harness.View.Build();

            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual("Assets", harness.Dialogs.ReceivedAssetFolder);
        }

        [Test]
        public void OnBrowseAssetClicked_Cancel_ChangesNothing()
        {
            var harness = new ViewHarness();
            harness.State.AssetPath = "Assets/Custom/Item.asset";
            harness.View.Build();
            harness.Dialogs.AssetToReturn = null;

            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual("Assets/Custom/Item.asset", harness.State.AssetPath);
        }

        [Test]
        public void Refresh_UnknownSelectedType_ClearsDropdownWithoutChangingModel()
        {
            var harness = new ViewHarness();
            harness.State.SelectedTypeName = "Missing.Type";

            var root = harness.View.Build();
            var dropdown = root.Q<DropdownField>(MasterToolView.MasterTypeFieldName);
            var boxes = root.Q(MasterToolView.InputProblemsName).Query<HelpBox>().ToList();

            Assert.AreEqual(-1, dropdown.index);
            Assert.AreEqual("Missing.Type", harness.State.SelectedTypeName);
            Assert.IsTrue(boxes.Any(box => box.text.Contains("見つかりません")));
        }

        [Test]
        public void Build_Twice_ReturnsDistinctTrees()
        {
            var harness = new ViewHarness();

            var first = harness.View.Build();
            var second = harness.View.Build();

            Assert.AreNotSame(first, second);
            Assert.IsNotNull(second.Q<DropdownField>(MasterToolView.MasterTypeFieldName));
        }

        [Test]
        public void Constructor_NullModel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MasterToolView(null));
        }

        sealed class ViewHarness
        {
            public readonly HashSet<string> ExistingFiles = new HashSet<string>(StringComparer.Ordinal);
            public Func<string, Type> Lookup = _ => null;
            public readonly MasterToolState State = new MasterToolState();
            public readonly MasterTypeCatalog Catalog;
            public readonly MasterToolModel Model;
            public readonly FakeFileDialogs Dialogs = new FakeFileDialogs();
            public readonly MasterToolView View;

            public ViewHarness()
            {
                Catalog = new MasterTypeCatalog(type =>
                    type == typeof(TestItemMaster) || type == typeof(TestQuestMaster));
                Model = new MasterToolModel(State, Catalog, ExistingFiles.Contains, path => Lookup(path));
                View = new MasterToolView(Model, Dialogs);
            }
        }

        sealed class FakeFileDialogs : IFileDialogs
        {
            public string JsonToReturn = string.Empty;
            public string AssetToReturn = string.Empty;
            public string ReceivedJsonDirectory;
            public string ReceivedSuggestedName;
            public string ReceivedAssetFolder;

            public string OpenJsonFile(string initialDirectory)
            {
                ReceivedJsonDirectory = initialDirectory;
                return JsonToReturn;
            }

            public string SaveAssetFile(string suggestedFileName, string initialAssetFolder)
            {
                ReceivedSuggestedName = suggestedFileName;
                ReceivedAssetFolder = initialAssetFolder;
                return AssetToReturn;
            }
        }
    }
}
