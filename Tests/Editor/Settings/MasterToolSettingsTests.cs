using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MasterDataTool.EditorTools.Settings;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterToolSettings"/> のテストです。EditorPrefs には触れません。
    /// </summary>
    public sealed class MasterToolSettingsTests
    {
        const string ProjectId = "project-a";

        [Test]
        public void ComputeProjectId_IsStableHex()
        {
            var first = MasterToolSettings.ComputeProjectId(@"C:\Projects\Alpha");
            var second = MasterToolSettings.ComputeProjectId(@"C:\Projects\Alpha");
            var other = MasterToolSettings.ComputeProjectId(@"C:\Projects\Beta");

            Assert.AreEqual(first, second);
            Assert.AreNotEqual(first, other);
            Assert.AreEqual(16, first.Length);
            Assert.IsTrue(Regex.IsMatch(first, "^[0-9a-f]{16}$"));
            Assert.IsTrue(Regex.IsMatch(other, "^[0-9a-f]{16}$"));
        }

        [Test]
        public void Save_KeysIncludePrefixAndProjectId()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);

            settings.Save(new MasterToolState
            {
                JsonPath = "C:/data/items.json",
                AssetPath = "Assets/MasterData/Item.asset",
                SelectedTypeName = "Sample.ItemMaster",
            });

            CollectionAssert.AreEquivalent(
                new[]
                {
                    Key(ProjectId, ".JsonPath"),
                    Key(ProjectId, ".AssetPath"),
                    Key(ProjectId, ".MasterType"),
                },
                store.StringKeys);
            foreach (var key in store.StringKeys)
            {
                StringAssert.StartsWith(MasterDataToolInfo.SettingsKeyPrefix + ProjectId, key);
            }
        }

        [Test]
        public void Save_DifferentProjectId_DoesNotMix()
        {
            var store = new FakeSettingsStore();
            var left = new MasterToolSettings(store, "alpha");
            var right = new MasterToolSettings(store, "beta");
            left.Save(new MasterToolState
            {
                JsonPath = "left.json",
                AssetPath = "Assets/Left.asset",
                SelectedTypeName = "Left.Master",
            });

            var state = new MasterToolState
            {
                JsonPath = "keep-json",
                AssetPath = "keep-asset",
                SelectedTypeName = "keep-type",
            };
            right.LoadInto(state, _ => true);

            Assert.AreEqual("keep-json", state.JsonPath);
            Assert.AreEqual("keep-asset", state.AssetPath);
            Assert.AreEqual("keep-type", state.SelectedTypeName);
        }

        [Test]
        public void Save_WritesThreeFields()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);
            settings.Save(new MasterToolState
            {
                JsonPath = "C:/data/items.json",
                AssetPath = "Assets/MasterData/Item.asset",
                SelectedTypeName = "Sample.ItemMaster",
            });

            Assert.AreEqual(3, store.SetStringCalls);
            AssertStored(store, ".JsonPath", "C:/data/items.json");
            AssertStored(store, ".AssetPath", "Assets/MasterData/Item.asset");
            AssertStored(store, ".MasterType", "Sample.ItemMaster");
        }

        [Test]
        public void Save_SkipsUnchangedValues()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);
            var state = new MasterToolState
            {
                JsonPath = "C:/data/items.json",
                AssetPath = "Assets/MasterData/Item.asset",
                SelectedTypeName = "Sample.ItemMaster",
            };

            settings.Save(state);
            settings.Save(state);

            Assert.AreEqual(3, store.SetStringCalls);

            state.JsonPath = "C:/data/other.json";
            settings.Save(state);

            Assert.AreEqual(4, store.SetStringCalls);
            Assert.AreEqual(Key(ProjectId, ".JsonPath"), store.WrittenStringKeys[3]);
        }

        [Test]
        public void Save_WritesEmptyString()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);
            settings.Save(new MasterToolState
            {
                JsonPath = string.Empty,
                AssetPath = null,
                SelectedTypeName = null,
            });

            Assert.AreEqual(3, store.SetStringCalls);
            AssertStored(store, ".JsonPath", string.Empty);
            AssertStored(store, ".AssetPath", string.Empty);
            AssertStored(store, ".MasterType", string.Empty);
        }

        [Test]
        public void Save_DoesNotWriteLastJsonDirectory()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);
            settings.Save(new MasterToolState
            {
                JsonPath = "C:/data/items.json",
                AssetPath = "Assets/MasterData/Item.asset",
                SelectedTypeName = "Sample.ItemMaster",
                LastJsonDirectory = "C:/keep",
            });

            Assert.AreEqual(3, store.StringKeys.Count);
            foreach (var value in store.StringValues)
            {
                Assert.AreNotEqual("C:/keep", value);
            }
        }

        [Test]
        public void LoadInto_AppliesOnlyExistingKeys()
        {
            var store = new FakeSettingsStore();
            store.SetString(Key(ProjectId, ".JsonPath"), "loaded.json");
            var settings = new MasterToolSettings(store, ProjectId);
            var state = new MasterToolState
            {
                JsonPath = "keep-json",
                AssetPath = "keep-asset",
                SelectedTypeName = "keep-type",
                LastJsonDirectory = "C:/session",
            };

            settings.LoadInto(state, _ => true);

            Assert.AreEqual("loaded.json", state.JsonPath);
            Assert.AreEqual("keep-asset", state.AssetPath);
            Assert.AreEqual("keep-type", state.SelectedTypeName);
            Assert.AreEqual("C:/session", state.LastJsonDirectory);
            Assert.IsFalse(state.SettingsRestored);
        }

        [Test]
        public void LoadInto_DoesNotSuggestAssetPath()
        {
            var typeName = typeof(TestItemMaster).FullName;
            var store = new FakeSettingsStore();
            store.SetString(Key(ProjectId, ".JsonPath"), "C:/only.json");
            var settings = new MasterToolSettings(store, ProjectId);
            var jsonOnly = new MasterToolState();

            settings.LoadInto(jsonOnly, name => name == typeName);

            Assert.AreEqual("C:/only.json", jsonOnly.JsonPath);
            Assert.IsNull(jsonOnly.AssetPath);
            Assert.IsNull(jsonOnly.SelectedTypeName);

            var typeOnlyStore = new FakeSettingsStore();
            typeOnlyStore.SetString(Key(ProjectId, ".MasterType"), typeName);
            var typeOnly = new MasterToolState();
            new MasterToolSettings(typeOnlyStore, ProjectId).LoadInto(typeOnly, name => name == typeName);

            Assert.AreEqual(typeName, typeOnly.SelectedTypeName);
            Assert.IsNull(typeOnly.AssetPath);
            Assert.IsNull(typeOnly.JsonPath);
        }

        [Test]
        public void LoadInto_MissingType_ClearsOnlySelectedTypeName()
        {
            var store = new FakeSettingsStore();
            store.SetString(Key(ProjectId, ".JsonPath"), "C:/data/items.json");
            store.SetString(Key(ProjectId, ".AssetPath"), "Assets/MasterData/Item.asset");
            store.SetString(Key(ProjectId, ".MasterType"), "Missing.Type");
            var settings = new MasterToolSettings(store, ProjectId);
            var state = new MasterToolState { SelectedTypeName = "Old.Type" };

            settings.LoadInto(state, _ => false);

            Assert.AreEqual(string.Empty, state.SelectedTypeName);
            Assert.AreEqual("C:/data/items.json", state.JsonPath);
            Assert.AreEqual("Assets/MasterData/Item.asset", state.AssetPath);
        }

        [Test]
        public void RestoreIfNeeded_LoadsAndMarksRestored()
        {
            var store = new FakeSettingsStore();
            store.SetString(Key(ProjectId, ".JsonPath"), "from-store.json");
            var settings = new MasterToolSettings(store, ProjectId);
            var state = new MasterToolState();

            settings.RestoreIfNeeded(state, _ => true);

            Assert.AreEqual("from-store.json", state.JsonPath);
            Assert.IsTrue(state.SettingsRestored);
        }

        [Test]
        public void RestoreIfNeeded_WhenAlreadyRestored_KeepsEmptyState()
        {
            var store = new FakeSettingsStore();
            store.SetString(Key(ProjectId, ".JsonPath"), "from-store.json");
            store.SetString(Key(ProjectId, ".AssetPath"), "Assets/Old.asset");
            store.SetString(Key(ProjectId, ".MasterType"), "Old.Type");
            var settings = new MasterToolSettings(store, ProjectId);
            var state = new MasterToolState { SettingsRestored = true };

            settings.RestoreIfNeeded(state, _ => true);

            Assert.IsNull(state.JsonPath);
            Assert.IsNull(state.AssetPath);
            Assert.IsNull(state.SelectedTypeName);
            Assert.IsTrue(state.SettingsRestored);
        }

        [Test]
        public void LogToConsole_DefaultsToTrueAndSkipsSameValue()
        {
            var store = new FakeSettingsStore();
            var settings = new MasterToolSettings(store, ProjectId);

            Assert.IsTrue(settings.LogToConsole);
            settings.LogToConsole = true;

            Assert.AreEqual(0, store.SetBoolCalls);

            settings.LogToConsole = false;
            Assert.IsFalse(settings.LogToConsole);
            Assert.AreEqual(1, store.SetBoolCalls);
            Assert.IsTrue(store.TryGetBool(Key(ProjectId, ".LogToConsole"), out var stored));
            Assert.IsFalse(stored);

            settings.LogToConsole = false;
            Assert.AreEqual(1, store.SetBoolCalls);

            settings.LogToConsole = true;
            Assert.IsTrue(settings.LogToConsole);
            Assert.AreEqual(2, store.SetBoolCalls);
        }

        static void AssertStored(FakeSettingsStore store, string suffix, string expected)
        {
            Assert.IsTrue(store.TryGetString(Key(ProjectId, suffix), out var value));
            Assert.AreEqual(expected, value);
        }

        static string Key(string projectId, string suffix)
        {
            return MasterDataToolInfo.SettingsKeyPrefix + projectId + suffix;
        }

        /// <summary>
        /// メモリ上の設定ストアです。テスト間で共有します。
        /// </summary>
        internal sealed class FakeSettingsStore : ISettingsStore
        {
            readonly Dictionary<string, string> strings = new Dictionary<string, string>(StringComparer.Ordinal);
            readonly Dictionary<string, bool> bools = new Dictionary<string, bool>();

            public int SetStringCalls { get; private set; }

            public int SetBoolCalls { get; private set; }

            public List<string> WrittenStringKeys { get; } = new List<string>();

            public ICollection<string> StringKeys => strings.Keys;

            public ICollection<string> StringValues => strings.Values;

            /// <inheritdoc />
            public bool TryGetString(string key, out string value)
            {
                return strings.TryGetValue(key, out value);
            }

            /// <inheritdoc />
            public void SetString(string key, string value)
            {
                SetStringCalls++;
                WrittenStringKeys.Add(key);
                strings[key] = value ?? string.Empty;
            }

            /// <inheritdoc />
            public bool TryGetBool(string key, out bool value)
            {
                return bools.TryGetValue(key, out value);
            }

            /// <inheritdoc />
            public void SetBool(string key, bool value)
            {
                SetBoolCalls++;
                bools[key] = value;
            }
        }
    }
}
