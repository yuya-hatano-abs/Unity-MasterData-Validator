using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterTable"/> のテストです。
    /// </summary>
    public class MasterTableTests
    {
        const string ItemJsonTwo =
            "{\n  \"records\": [\n    { \"id\": 1, \"name\": \"A\" },\n    { \"id\": 2, \"name\": \"B\" },\n    { \"id\": 3, \"name\": \"C\" }\n  ]\n}";

        string tempFolder;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [SetUp]
        public void SetUp()
        {
            TempAssetFolder.CleanupRootIfPresent();
            tempFolder = TempAssetFolder.CreateUniqueFolder();
        }

        [TearDown]
        public void TearDown()
        {
            TempAssetFolder.DeleteFolder(tempFolder);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [Test]
        public void FromAsset_InMemoryItemMaster_MapsFields()
        {
            var master = ScriptableObject.CreateInstance<TestItemMaster>();
            try
            {
                master.ReplaceRecords(new[]
                {
                    new TestItemRecord(1, "A"),
                    new TestItemRecord(2, "B"),
                    new TestItemRecord(3, "C"),
                });

                var table = MasterTable.FromAsset(master);

                Assert.AreEqual("TestItemMaster", table.Name);
                Assert.AreEqual(typeof(TestItemMaster), table.MasterType);
                Assert.AreEqual(typeof(TestItemRecord), table.RecordType);
                Assert.AreEqual(MasterIdKind.Int, table.IdKind);
                Assert.AreEqual(3, table.Records.Count);
                Assert.AreEqual("A", ((TestItemRecord)table.Records[0]).Name);
                Assert.IsNull(table.AssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(master);
            }
        }

        [Test]
        public void FromAsset_InMemoryQuestMaster_IdKindIsString()
        {
            var master = ScriptableObject.CreateInstance<TestQuestMaster>();
            try
            {
                master.ReplaceRecords(new[] { new TestQuestRecord("Q1", "T") });
                var table = MasterTable.FromAsset(master);
                Assert.AreEqual(MasterIdKind.String, table.IdKind);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(master);
            }
        }

        [Test]
        public void FromAsset_PersistedAsset_HasAssetPath()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "Item.asset");
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwo);
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            var table = MasterTable.FromAsset(loaded);

            Assert.AreEqual(path, table.AssetPath);
        }

        [Test]
        public void FromAsset_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MasterTable.FromAsset(null));
        }

        [Test]
        public void FromImport_MapsFieldsAndAssetPath()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwo);
            var table = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, "Assets/X/Item.asset");

            Assert.AreEqual("TestItemMaster", table.Name);
            Assert.AreEqual(typeof(TestItemMaster), table.MasterType);
            Assert.AreEqual(typeof(TestItemRecord), table.RecordType);
            Assert.AreEqual(MasterIdKind.Int, table.IdKind);
            Assert.AreEqual(3, table.Records.Count);
            Assert.AreEqual("Assets/X/Item.asset", table.AssetPath);
        }

        [Test]
        public void FromImport_NullAssetPath_Succeeds()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwo);
            var table = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, null);
            Assert.IsNull(table.AssetPath);
        }

        [Test]
        public void FromImport_EmptyRecords_InfersIdKindFromType()
        {
            const string json = "{ \"records\": [] }";
            var import = MasterJsonImporter.Import<TestItemRecord>(json);
            var intTable = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, null);
            Assert.AreEqual(MasterIdKind.Int, intTable.IdKind);
            Assert.AreEqual(0, intTable.Records.Count);

            var questImport = MasterJsonImporter.Import<TestQuestRecord>(json);
            var stringTable = MasterTable.FromImport<TestQuestMaster, TestQuestRecord>(questImport, null);
            Assert.AreEqual(MasterIdKind.String, stringTable.IdKind);
        }

        [Test]
        public void FromImport_FailedImport_HasEmptyRecords()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>("{");
            var table = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, null);
            Assert.AreEqual(0, table.Records.Count);
        }

        [Test]
        public void FromImport_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MasterTable.FromImport<TestItemMaster, TestItemRecord>(null, null));
        }

        [Test]
        public void Constructor_IsSnapshotOfList()
        {
            var record = new TestItemRecord(1, "A");
            var list = new List<IMasterRecord> { record };
            var table = new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                list,
                null);

            list.Clear();
            Assert.AreEqual(1, table.Records.Count);
            Assert.AreSame(record, table.Records[0]);
        }

        [Test]
        public void Constructor_ValidatesArguments()
        {
            var records = new List<IMasterRecord> { null };
            Assert.Throws<ArgumentNullException>(() => new MasterTable(
                null, typeof(TestItemMaster), typeof(TestItemRecord), MasterIdKind.Int, records, null));
            Assert.Throws<ArgumentException>(() => new MasterTable(
                string.Empty, typeof(TestItemMaster), typeof(TestItemRecord), MasterIdKind.Int, records, null));
            Assert.Throws<ArgumentNullException>(() => new MasterTable(
                "X", typeof(TestItemMaster), null, MasterIdKind.Int, records, null));
            Assert.Throws<ArgumentNullException>(() => new MasterTable(
                "X", typeof(TestItemMaster), typeof(TestItemRecord), MasterIdKind.Int, null, null));

            var withNullElement = new MasterTable(
                "X",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                records,
                null);
            Assert.AreEqual(1, withNullElement.Records.Count);
            Assert.IsNull(withNullElement.Records[0]);
        }
    }
}
