using System.Collections.Generic;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Tests.NameClashA;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ClashA = MasterDataTool.EditorTools.Tests.NameClashA.ClashMaster;
using ClashB = MasterDataTool.EditorTools.Tests.NameClashB.ClashMaster;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ProjectMasterTableCollector"/> のテストです。
    /// </summary>
    public sealed class ProjectMasterTableCollectorTests
    {
        readonly List<Object> owned = new List<Object>();
        string folder;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                {
                    Object.DestroyImmediate(owned[i]);
                }
            }

            owned.Clear();
            TempAssetFolder.DeleteFolder(folder);
            folder = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [Test]
        public void Collect_OneAssetPerType_ReturnsTablesSortedByName()
        {
            var item = OwnItem(1, "sword");
            var quest = OwnQuest("q1", "first");
            var collected = Collect(item, quest);

            Assert.AreEqual(0, collected.Warnings.Count);
            Assert.AreEqual(2, collected.Tables.Count);
            Assert.AreEqual("TestItemMaster", collected.Tables[0].Name);
            Assert.AreEqual("TestQuestMaster", collected.Tables[1].Name);
            Assert.AreEqual(MasterIdKind.Int, collected.Tables[0].IdKind);
            Assert.AreEqual(MasterIdKind.String, collected.Tables[1].IdKind);
            Assert.AreEqual("1", collected.Tables[0].Records[0].Key);
            Assert.AreEqual("q1", collected.Tables[1].Records[0].Key);
        }

        [Test]
        public void Collect_DuplicateType_ExcludesTypeAndWarns()
        {
            var collected = Collect(OwnItem(1, "a"), OwnItem(2, "b"), OwnQuest("q", "t"));

            Assert.AreEqual(1, collected.Tables.Count);
            Assert.AreEqual(typeof(TestQuestMaster), collected.Tables[0].MasterType);
            Assert.AreEqual(1, collected.Warnings.Count);
            var warning = collected.Warnings[0];
            Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
            Assert.AreEqual(ValidationIssueKind.MultipleMasterAssets, warning.Kind);
            Assert.AreEqual(ProjectMasterTableCollector.ValidatorId, warning.ValidatorId);
            Assert.AreEqual("TestItemMaster", warning.MasterName);
            StringAssert.Contains("2", warning.Message);
            StringAssert.Contains("(未保存)", warning.Message);
        }

        [Test]
        public void Collect_SeveralDuplicateTypes_SortsWarningsByName()
        {
            var collected = Collect(OwnQuest("q", "t"), OwnQuest("q2", "u"), OwnItem(1, "a"), OwnItem(2, "b"));

            Assert.AreEqual(0, collected.Tables.Count);
            Assert.AreEqual(2, collected.Warnings.Count);
            Assert.AreEqual("TestItemMaster", collected.Warnings[0].MasterName);
            Assert.AreEqual("TestQuestMaster", collected.Warnings[1].MasterName);
            Assert.LessOrEqual(
                string.CompareOrdinal(collected.Warnings[0].MasterName, collected.Warnings[1].MasterName),
                0);
        }

        [Test]
        public void Collect_NullElements_AreIgnored()
        {
            var collected = Collect(null, OwnItem(1, "a"), null);

            Assert.AreEqual(1, collected.Tables.Count);
            Assert.AreEqual(typeof(TestItemMaster), collected.Tables[0].MasterType);
            Assert.AreEqual(0, collected.Warnings.Count);
        }

        [Test]
        public void Collect_EmptySource_IsEmpty()
        {
            var collected = new ProjectMasterTableCollector(new FakeSource()).Collect();

            Assert.AreEqual(0, collected.Tables.Count);
            Assert.AreEqual(0, collected.Warnings.Count);
        }

        [Test]
        public void Constructor_WithoutSource_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new ProjectMasterTableCollector());
        }

        [Test]
        public void AssetDatabaseSource_SearchFolder_ReturnsSortedAssets()
        {
            folder = TempAssetFolder.CreateUniqueFolder();
            var itemA = TempAssetFolder.AssetPath(folder, "item-a.asset");
            var itemB = TempAssetFolder.AssetPath(folder, "item-b.asset");
            var quest = TempAssetFolder.AssetPath(folder, "quest.asset");
            SaveItem(itemA, 1, "a");
            SaveItem(itemB, 2, "b");
            SaveQuest(quest, "q", "t");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var source = new AssetDatabaseMasterAssetSource(new[] { folder });
            var loaded = source.LoadAll();

            Assert.AreEqual(3, loaded.Count);
            Assert.AreEqual(itemA, AssetDatabase.GetAssetPath(loaded[0]));
            Assert.AreEqual(itemB, AssetDatabase.GetAssetPath(loaded[1]));
            Assert.AreEqual(quest, AssetDatabase.GetAssetPath(loaded[2]));

            var collected = new ProjectMasterTableCollector(source).Collect();
            Assert.AreEqual(1, collected.Tables.Count);
            Assert.AreEqual(quest, collected.Tables[0].AssetPath);
            Assert.AreEqual(1, collected.Warnings.Count);
            StringAssert.Contains(itemA, collected.Warnings[0].Message);
            StringAssert.Contains(itemB, collected.Warnings[0].Message);
        }

        [Test]
        public void Collect_NameClash_ExcludesBothSingletons()
        {
            var collected = Collect(OwnClashA(1), OwnClashB(2), OwnItem(1, "a"));

            Assert.AreEqual(1, collected.Tables.Count);
            Assert.AreEqual(typeof(TestItemMaster), collected.Tables[0].MasterType);
            Assert.AreEqual(1, collected.Warnings.Count);
            var warning = collected.Warnings[0];
            Assert.AreEqual(ValidationIssueKind.AmbiguousMasterName, warning.Kind);
            Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
            Assert.AreEqual(ProjectMasterTableCollector.ValidatorId, warning.ValidatorId);
            Assert.AreEqual("ClashMaster", warning.MasterName);
            Assert.IsNull(warning.AssetPath);
            Assert.AreEqual(-1, warning.RecordIndex);
            Assert.IsNull(warning.RecordKey);
            Assert.IsNull(warning.FieldName);
            Assert.AreEqual(0, warning.LineNumber);
            StringAssert.Contains(typeof(ClashA).FullName, warning.Message);
            StringAssert.Contains(typeof(ClashB).FullName, warning.Message);
        }

        [Test]
        public void Collect_NameClash_DuplicateA_LeavesSingleB()
        {
            var collected = Collect(OwnClashA(1), OwnClashA(2), OwnClashB(3));

            Assert.AreEqual(1, collected.Tables.Count);
            Assert.AreEqual(typeof(ClashB), collected.Tables[0].MasterType);
            Assert.AreEqual(1, collected.Warnings.Count);
            Assert.AreEqual(ValidationIssueKind.MultipleMasterAssets, collected.Warnings[0].Kind);
            Assert.AreEqual("ClashMaster", collected.Warnings[0].MasterName);
            StringAssert.Contains("2", collected.Warnings[0].Message);
            Assert.IsFalse(collected.Warnings[0].Message.Contains(typeof(ClashB).FullName));
        }

        ProjectTables Collect(params MasterDataBase[] assets)
        {
            var source = new FakeSource();
            for (var i = 0; i < assets.Length; i++)
            {
                source.Assets.Add(assets[i]);
            }

            return new ProjectMasterTableCollector(source).Collect();
        }

        TestItemMaster OwnItem(int id, string name)
        {
            var asset = ScriptableObject.CreateInstance<TestItemMaster>();
            asset.ReplaceRecords(new[] { new TestItemRecord(id, name) });
            owned.Add(asset);
            return asset;
        }

        TestQuestMaster OwnQuest(string id, string title)
        {
            var asset = ScriptableObject.CreateInstance<TestQuestMaster>();
            asset.ReplaceRecords(new[] { new TestQuestRecord(id, title) });
            owned.Add(asset);
            return asset;
        }

        ClashA OwnClashA(int id)
        {
            var asset = ScriptableObject.CreateInstance<ClashA>();
            asset.ReplaceRecords(new[] { new ClashRecord(id) });
            owned.Add(asset);
            return asset;
        }

        ClashB OwnClashB(int id)
        {
            var asset = ScriptableObject.CreateInstance<ClashB>();
            asset.ReplaceRecords(new[] { new NameClashB.ClashRecord(id) });
            owned.Add(asset);
            return asset;
        }

        static void SaveItem(string path, int id, string name)
        {
            var asset = ScriptableObject.CreateInstance<TestItemMaster>();
            asset.ReplaceRecords(new[] { new TestItemRecord(id, name) });
            AssetDatabase.CreateAsset(asset, path);
        }

        static void SaveQuest(string path, string id, string title)
        {
            var asset = ScriptableObject.CreateInstance<TestQuestMaster>();
            asset.ReplaceRecords(new[] { new TestQuestRecord(id, title) });
            AssetDatabase.CreateAsset(asset, path);
        }

        sealed class FakeSource : IMasterAssetSource
        {
            public readonly List<MasterDataBase> Assets = new List<MasterDataBase>();

            public IReadOnlyList<MasterDataBase> LoadAll()
            {
                return Assets;
            }
        }
    }
}
