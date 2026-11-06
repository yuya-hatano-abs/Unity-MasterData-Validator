using System;
using System.IO;
using System.Linq;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterAssetSaver"/> のテストです。
    /// </summary>
    public class MasterAssetSaverTests
    {
        const string ItemJsonTwoRecords =
            "{\n  \"records\": [\n    { \"id\": 1, \"name\": \"Potion\" },\n    { \"id\": 2, \"name\": \"Ether\" }\n  ]\n}";

        const string ItemJsonReordered =
            "{\n  \"records\": [\n    { \"id\": 2, \"name\": \"Ether\" },\n    { \"id\": 1, \"name\": \"Potion\" }\n  ]\n}";

        const string ItemJsonOneNameChanged =
            "{\n  \"records\": [\n    { \"id\": 1, \"name\": \"PotionX\" },\n    { \"id\": 2, \"name\": \"Ether\" }\n  ]\n}";

        const string QuestJson =
            "{ \"records\": [ { \"id\": \"Quest_A\", \"title\": \"First\" } ] }";

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
            tempFolder = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        static void AssertPersisted<TMaster, TRecord>(string assetPath, int expectedCount, Func<TRecord, bool> assertFirst)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<TMaster>(assetPath);
            Assert.NotNull(loaded);
            Assert.AreEqual(expectedCount, loaded.Count);
            Assert.IsTrue(assertFirst(loaded.Records[0]));
        }

        [Test]
        public void Save_NewItemMaster_Created()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(SaveOutcome.Created, result.Outcome);
            Assert.AreEqual(-1, result.PreviousRecordCount);
            Assert.AreEqual(2, result.NewRecordCount);
            Assert.NotNull(result.Asset);
            Assert.AreEqual(path, result.AssetPath);
            Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)));
            AssertPersisted<TestItemMaster, TestItemRecord>(path, 2, r => r.Id == 1 && r.Name == "Potion");
        }

        [Test]
        public void Save_NewQuestMaster_Created()
        {
            var import = MasterJsonImporter.Import<TestQuestRecord>(QuestJson);
            var path = TempAssetFolder.AssetPath(tempFolder, "TestQuestMaster.asset");
            var result = MasterAssetSaver.Save<TestQuestMaster, TestQuestRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Created, result.Outcome);
            AssertPersisted<TestQuestMaster, TestQuestRecord>(path, 1, r => r.Id == "Quest_A" && r.Title == "First");
        }

        [Test]
        public void Save_CreatesNestedFolders()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var path = tempFolder + "/nested/deep/TestItemMaster.asset";
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Created, result.Outcome);
            Assert.IsTrue(AssetDatabase.IsValidFolder(tempFolder + "/nested/deep"));
        }

        [Test]
        public void Save_CreateFoldersFalse_FolderNotFound()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var path = tempFolder + "/missing/TestItemMaster.asset";
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { CreateFolders = false, ExistingAsset = OverwritePolicy.Overwrite });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(SaveIssueKind.FolderNotFound, result.Issues[0].Kind);
            Assert.IsFalse(File.Exists(ToAbsolutePath(path)));
        }

        [Test]
        public void Save_Overwrite_Updated()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var first = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                first,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var guidBefore = AssetDatabase.AssetPathToGUID(path);
            var second = MasterJsonImporter.Import<TestItemRecord>(ItemJsonOneNameChanged);
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                second,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Updated, result.Outcome);
            Assert.AreEqual(2, result.PreviousRecordCount);
            Assert.AreEqual(2, result.NewRecordCount);
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(path));
            AssertPersisted<TestItemMaster, TestItemRecord>(path, 2, r => r.Name == "PotionX");
        }

        [Test]
        public void Save_SameContent_Overwrite_Unchanged()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var absolute = ToAbsolutePath(path);
            var bytesBefore = File.ReadAllBytes(absolute);
            var asset = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            Assert.IsFalse(EditorUtility.IsDirty(asset));

            var again = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Unchanged, again.Outcome);
            Assert.IsFalse(EditorUtility.IsDirty(asset));
            CollectionAssert.AreEqual(bytesBefore, File.ReadAllBytes(absolute));
        }

        [Test]
        public void Save_AfterImportAssetForceUpdate_SameContent_Unchanged()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloadedImport = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                reloadedImport,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Unchanged, result.Outcome);
        }

        [Test]
        public void Save_ReorderedRecords_Overwrite_Updated()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonReordered),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Updated, result.Outcome);
        }

        [Test]
        public void Save_SingleFieldChange_Overwrite_Updated()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonOneNameChanged),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Updated, result.Outcome);
        }

        [Test]
        public void Save_ExistingAsset_DefaultFailPolicy_AssetExists()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var originalName = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path).Records[0].Name;
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonOneNameChanged),
                path,
                null);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(SaveIssueKind.AssetExists, result.Issues[0].Kind);
            Assert.AreEqual(originalName, AssetDatabase.LoadAssetAtPath<TestItemMaster>(path).Records[0].Name);
        }

        [Test]
        public void Save_AssetTypeMismatch_DoesNotModifyExisting()
        {
            var itemPath = TempAssetFolder.AssetPath(tempFolder, "Mixed.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                itemPath,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var questImport = MasterJsonImporter.Import<TestQuestRecord>(QuestJson);
            var result = MasterAssetSaver.Save<TestQuestMaster, TestQuestRecord>(
                questImport,
                itemPath,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveIssueKind.AssetTypeMismatch, result.Issues[0].Kind);
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<TestItemMaster>(itemPath).TryGetById(1, out _));
        }

        [Test]
        public void Save_ImportHasErrors_DoesNotCreateAsset()
        {
            const string json = "{ \"records\": [ { \"id\": 1, \"name\": 1 } ] }";
            var import = MasterJsonImporter.Import<TestItemRecord>(json);
            var path = TempAssetFolder.AssetPath(tempFolder, "Bad.asset");
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(import, path);

            Assert.AreEqual(SaveIssueKind.ImportHasErrors, result.Issues[0].Kind);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(path));
        }

        [Test]
        public void Save_ImportNotSucceeded_DoesNotCreateAsset()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>("{");
            var path = TempAssetFolder.AssetPath(tempFolder, "Bad.asset");
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(import, path);

            Assert.AreEqual(SaveIssueKind.ImportNotSucceeded, result.Issues[0].Kind);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(path));
        }

        [Test]
        public void Save_WarningsOnly_Created()
        {
            const string json = "{ \"records\": [ { \"id\": 1, \"name\": \"A\", \"extra\": 1 } ] }";
            var import = MasterJsonImporter.Import<TestItemRecord>(json);
            Assert.IsTrue(import.HasWarnings);
            Assert.IsFalse(import.HasErrors);

            var path = TempAssetFolder.AssetPath(tempFolder, "Warn.asset");
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            Assert.AreEqual(SaveOutcome.Created, result.Outcome);
        }

        [Test]
        public void Save_InvalidAssetPath_DoesNotCreate()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var path = TempAssetFolder.AssetPath(tempFolder, "Ok.asset");
            AssertInvalidPath(import, string.Empty);
            AssertInvalidPath(import, null);
            AssertInvalidPath(import, "Packages/x/y.asset");
            AssertInvalidPath(import, "Assets/../outside.asset");
            AssertInvalidPath(import, "Assets/foo.txt");
            AssertInvalidPath(import, "Assets/foo\\bar.asset");
            AssertInvalidPath(import, "Assets/foo/");
            AssertInvalidPath(import, "Assets//foo.asset");
            AssertInvalidPath(import, ToAbsolutePath(path));
            AssertInvalidPath(import, "Assets/.asset");
        }

        void AssertInvalidPath(ImportResult<TestItemRecord> import, string badPath)
        {
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(import, badPath);
            Assert.IsFalse(result.Succeeded, "path: " + badPath);
            Assert.AreEqual(SaveIssueKind.InvalidAssetPath, result.Issues[0].Kind);
            Assert.IsFalse(string.IsNullOrEmpty(result.Issues[0].Message));
        }

        [Test]
        public void Save_InvalidMasterType_Fails()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords);
            var path = TempAssetFolder.AssetPath(tempFolder, "Abstract.asset");
            var result = MasterAssetSaver.Save<AbstractTestItemMaster, TestItemRecord>(import, path);

            Assert.AreEqual(SaveIssueKind.InvalidMasterType, result.Issues[0].Kind);
        }

        [Test]
        public void Save_NullImportResult_ThrowsArgumentNullException()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "Null.asset");
            Assert.Throws<ArgumentNullException>(() =>
                MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(null, path));
        }

        [Test]
        public void Save_Update_DoesNotLeakTemporaryInstances()
        {
            var path = TempAssetFolder.AssetPath(tempFolder, "Leak.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var countBefore = Resources.FindObjectsOfTypeAll<TestItemMaster>().Length;
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonTwoRecords),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                MasterJsonImporter.Import<TestItemRecord>(ItemJsonOneNameChanged),
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            var countAfter = Resources.FindObjectsOfTypeAll<TestItemMaster>().Length;

            Assert.AreEqual(countBefore, countAfter);
        }

        [Test]
        public void Save_FailureIssues_HaveKindAndMessage()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>("{");
            var result = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                TempAssetFolder.AssetPath(tempFolder, "X.asset"));

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Count > 0);
            Assert.IsFalse(string.IsNullOrEmpty(result.Issues[0].Message));
        }

        abstract class AbstractTestItemMaster : MasterDataBase<TestItemRecord>
        {
        }
    }
}
