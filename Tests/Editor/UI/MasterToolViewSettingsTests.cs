using MasterDataTool.EditorTools.Settings;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// 設定ありの <see cref="MasterToolView"/> のテストです。EditorPrefs には触れません。
    /// </summary>
    public sealed class MasterToolViewSettingsTests
    {
        const string ProjectId = "view";

        [Test]
        public void Build_WithoutSettings_HasNoLogToggle()
        {
            var harness = new ViewHarness(null);

            var root = harness.View.Build();

            Assert.IsNull(root.Q<Toggle>(MasterToolView.LogToggleName));
            Assert.DoesNotThrow(() => harness.View.OnJsonPathChanged("C:/data/items.json"));
        }

        [Test]
        public void Build_WithoutStoredLog_ToggleIsTrue()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());

            var root = harness.View.Build();
            var toggle = root.Q<Toggle>(MasterToolView.LogToggleName);

            Assert.IsNotNull(toggle);
            Assert.AreEqual("Console にログを出す", toggle.text);
            Assert.IsTrue(toggle.value);
            Assert.IsNull(root.Q(MasterToolView.ResultsContainerName).Q<Toggle>(MasterToolView.LogToggleName));
            Assert.AreEqual(0, harness.Store.SetBoolCalls);
            Assert.AreEqual(0, harness.Store.SetStringCalls);
        }

        [Test]
        public void Build_StoredFalse_ToggleIsFalse()
        {
            var store = new MasterToolSettingsTests.FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);
            settings.LogToConsole = false;
            var writes = store.SetBoolCalls;
            var harness = new ViewHarness(store, settings);

            var root = harness.View.Build();

            Assert.IsFalse(root.Q<Toggle>(MasterToolView.LogToggleName).value);
            Assert.AreEqual(writes, store.SetBoolCalls);
        }

        [Test]
        public void ToggleChange_WritesStore()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            var root = harness.View.Build();
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                // Toggle.value は panel があるときだけ ChangeEvent を送る。
                window.position = new Rect(-32000f, -32000f, 80f, 80f);
                window.ShowUtility();
                window.rootVisualElement.Add(root);
                var toggle = root.Q<Toggle>(MasterToolView.LogToggleName);
                Assert.IsNotNull(toggle.panel);

                toggle.value = false;

                Assert.IsFalse(harness.Settings.LogToConsole);
                Assert.AreEqual(1, harness.Store.SetBoolCalls);
                Assert.IsTrue(harness.Store.TryGetBool(Key(".LogToConsole"), out var stored));
                Assert.IsFalse(stored);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void OnMasterTypeChanged_SavesThreeFields()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();

            harness.View.OnMasterTypeChanged(typeof(TestItemMaster).FullName);

            Assert.AreEqual(3, harness.Store.SetStringCalls);
            AssertStored(harness, ".MasterType", typeof(TestItemMaster).FullName);
            AssertStored(harness, ".JsonPath", string.Empty);
            AssertStored(harness, ".AssetPath", "Assets/MasterData/TestItemMaster.asset");
        }

        [Test]
        public void OnJsonPathChanged_SavesPath()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();

            harness.View.OnJsonPathChanged("C:/data/items.json");

            AssertStored(harness, ".JsonPath", "C:/data/items.json");
        }

        [Test]
        public void OnJsonPathChanged_Empty_SavesEmptyString()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();
            harness.View.OnJsonPathChanged("C:/data/items.json");

            harness.View.OnJsonPathChanged(string.Empty);

            AssertStored(harness, ".JsonPath", string.Empty);
        }

        [Test]
        public void OnAssetPathChanged_SavesPath()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();

            harness.View.OnAssetPathChanged("Assets/Custom/Item.asset");

            AssertStored(harness, ".AssetPath", "Assets/Custom/Item.asset");
        }

        [Test]
        public void Refresh_DoesNotWrite()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();
            var strings = harness.Store.SetStringCalls;
            var flags = harness.Store.SetBoolCalls;

            harness.View.Refresh();

            Assert.AreEqual(strings, harness.Store.SetStringCalls);
            Assert.AreEqual(flags, harness.Store.SetBoolCalls);
        }

        [Test]
        public void Browse_SelectionSaves_CancelDoesNot()
        {
            var harness = new ViewHarness(new MasterToolSettingsTests.FakeSettingsStore());
            harness.View.Build();
            harness.Dialogs.JsonToReturn = "C:/picked/items.json";

            harness.View.OnBrowseJsonClicked();

            AssertStored(harness, ".JsonPath", "C:/picked/items.json");
            var afterSelect = harness.Store.SetStringCalls;

            harness.Dialogs.JsonToReturn = null;
            harness.View.OnBrowseJsonClicked();

            Assert.AreEqual(afterSelect, harness.Store.SetStringCalls);
            AssertStored(harness, ".JsonPath", "C:/picked/items.json");

            harness.Dialogs.AssetToReturn = "Assets/Custom/Picked.asset";
            harness.View.OnBrowseAssetClicked();

            AssertStored(harness, ".AssetPath", "Assets/Custom/Picked.asset");
            var afterAsset = harness.Store.SetStringCalls;
            Assert.Greater(afterAsset, afterSelect);

            harness.Dialogs.AssetToReturn = string.Empty;
            harness.View.OnBrowseAssetClicked();

            Assert.AreEqual(afterAsset, harness.Store.SetStringCalls);
            AssertStored(harness, ".AssetPath", "Assets/Custom/Picked.asset");
        }

        static void AssertStored(ViewHarness harness, string suffix, string expected)
        {
            Assert.IsTrue(harness.Store.TryGetString(Key(suffix), out var value));
            Assert.AreEqual(expected, value);
        }

        static string Key(string suffix)
        {
            return MasterDataToolInfo.SettingsKeyPrefix + ProjectId + suffix;
        }

        sealed class ViewHarness
        {
            public readonly MasterToolSettingsTests.FakeSettingsStore Store;
            public readonly MasterToolSettings Settings;
            public readonly FakeFileDialogs Dialogs = new FakeFileDialogs();
            public readonly MasterToolView View;

            public ViewHarness(MasterToolSettingsTests.FakeSettingsStore store)
                : this(store, store == null ? null : new MasterToolSettings(store, ProjectId))
            {
            }

            public ViewHarness(MasterToolSettingsTests.FakeSettingsStore store, MasterToolSettings settings)
            {
                Store = store;
                Settings = settings;
                var state = new MasterToolState();
                var catalog = new MasterTypeCatalog(type => type == typeof(TestItemMaster));
                var model = new MasterToolModel(state, catalog, _ => false, _ => null);
                View = new MasterToolView(model, Dialogs, null, null, settings);
            }
        }

        sealed class FakeFileDialogs : IFileDialogs
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
    }
}
