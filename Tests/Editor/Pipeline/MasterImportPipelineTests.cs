using System;
using System.Collections.Generic;
using System.IO;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;
using ClashA = MasterDataTool.EditorTools.Tests.NameClashA.ClashMaster;
using ClashRecordA = MasterDataTool.EditorTools.Tests.NameClashA.ClashRecord;
using ClashB = MasterDataTool.EditorTools.Tests.NameClashB.ClashMaster;
using ClashRecordB = MasterDataTool.EditorTools.Tests.NameClashB.ClashRecord;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterImportPipeline"/> のテストです。確認ダイアログは呼びません。
    /// </summary>
    public sealed class MasterImportPipelineTests
    {
        const string ItemJson =
            "{ \"records\": [ { \"id\": 1, \"name\": \"Potion\" }, { \"id\": 2, \"name\": \"Ether\" } ] }";

        const string ItemJsonOne =
            "{ \"records\": [ { \"id\": 1, \"name\": \"Potion\" } ] }";

        const string QuestJson =
            "{ \"records\": [ { \"id\": \"Quest_A\", \"title\": \"First\" } ] }";

        const string RefJsonValid =
            "{ \"records\": [ { \"id\": 1, \"itemId\": 1, \"optionalItemId\": 0, \"questId\": \"\", \"itemIds\": [], \"questIds\": [], \"plainNumber\": 0 } ] }";

        const string RefJsonBroken =
            "{ \"records\": [ { \"id\": 1, \"itemId\": 99, \"optionalItemId\": 0, \"questId\": \"\", \"itemIds\": [], \"questIds\": [], \"plainNumber\": 0 } ] }";

        string folder;
        string jsonPath;
        FakeConfirm confirm;
        MasterImportPipeline pipeline;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [SetUp]
        public void SetUp()
        {
            TempAssetFolder.CleanupRootIfPresent();
            folder = TempAssetFolder.CreateUniqueFolder();
            jsonPath = Path.Combine(Path.GetTempPath(), "mdt-pipeline-" + Guid.NewGuid().ToString("N") + ".json");
            confirm = new FakeConfirm();
            pipeline = new MasterImportPipeline(confirm);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(jsonPath) && File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }

            jsonPath = null;
            TempAssetFolder.DeleteFolder(folder);
            folder = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TempAssetFolder.CleanupRootIfPresent();
        }

        [Test]
        public void Execute_NullRequest_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => pipeline.Execute(null, ProjectTables.Empty));
            Assert.AreEqual("request", exception.ParamName);
        }

        [Test]
        public void Execute_NullMasterType_InvalidRequest()
        {
            var result = ExecuteRaw(null, typeof(TestItemRecord), jsonPath, Asset("Item.asset"));
            AssertInvalid(result, "マスタ型が指定されていません。");
        }

        [Test]
        public void Execute_NullRecordType_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestItemMaster), null, jsonPath, Asset("Item.asset"));
            AssertInvalid(result, "レコード型が指定されていません。");
        }

        [Test]
        public void Execute_AbstractMaster_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestAbstractMaster), typeof(TestItemRecord), jsonPath, Asset("Item.asset"));
            AssertInvalid(result, "abstract");
        }

        [Test]
        public void Execute_MismatchedRecordType_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestItemMaster), typeof(TestQuestRecord), jsonPath, Asset("Item.asset"));
            AssertInvalid(result, "MasterDataBase<TestQuestRecord>");
        }

        [Test]
        public void Execute_RecordTypeNotMasterRecord_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestItemMaster), typeof(string), jsonPath, Asset("Item.asset"));
            AssertInvalid(result, "MasterRecordBase");
        }

        [Test]
        public void Execute_EmptyJsonPath_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestItemMaster), typeof(TestItemRecord), string.Empty, Asset("Item.asset"));
            AssertInvalid(result, "JSON ファイルのパスが空です。");
        }

        [Test]
        public void Execute_EmptyAssetPath_InvalidRequest()
        {
            var result = ExecuteRaw(typeof(TestItemMaster), typeof(TestItemRecord), jsonPath, string.Empty);
            AssertInvalid(result, "保存先のアセットパスが空です。");
        }

        [Test]
        public void Execute_Validate_Success_DoesNotSave()
        {
            Write(ItemJson);
            var path = Asset("Item.asset");
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(0, result.Validation.Issues.Count);
            Assert.IsTrue(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.AreEqual(string.Empty, result.Message);
            Assert.AreEqual(2, result.Table.Records.Count);
            Assert.AreEqual("Potion", ((TestItemRecord)result.Table.Records[0]).Name);
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(path));
        }

        [Test]
        public void Execute_UnknownField_ValidatedAndCanSave()
        {
            Write("{ \"records\": [ { \"id\": 1, \"name\": \"Potion\", \"extra\": 1 } ] }");
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            Assert.IsTrue(result.Validation.HasWarnings);
            Assert.IsFalse(result.Validation.HasErrors);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.UnknownField));
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsNull(result.Save);
        }

        [Test]
        public void Execute_BadJson_ImportFailed()
        {
            Write("{");
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ImportFailed, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.IsNotNull(result.Table);
            Assert.AreEqual(0, result.Table.Records.Count);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.ImportFailed));
            Assert.IsNull(FindKind(result.Validation, ValidationIssueKind.DuplicateId));
        }

        [Test]
        public void Execute_MissingFile_ImportFailed()
        {
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ImportFailed, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.IsNotNull(result.Table);
            Assert.AreEqual(0, result.Table.Records.Count);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.ImportFailed));
        }

        [Test]
        public void Execute_TypeMismatch_DoesNotSave()
        {
            Write("{ \"records\": [ { \"id\": \"x\", \"name\": \"Potion\" } ] }");
            var path = Asset("Item.asset");
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.TypeMismatch));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(path));
        }

        [Test]
        public void Execute_DuplicateId_ValidationFailed()
        {
            Write("{ \"records\": [ { \"id\": 1, \"name\": \"A\" }, { \"id\": 1, \"name\": \"B\" } ] }");
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.DuplicateId));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(Asset("Item.asset")));
        }

        [Test]
        public void Execute_MissingRequiredField()
        {
            Write("{ \"records\": [ { \"id\": \"r1\", \"name\": \"\", \"dropItemIds\": [1], \"tags\": [\"t\"], \"note\": \"\", \"questId\": \"q1\" } ] }");
            var quest = Table(
                "TestQuestMaster",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                new TestQuestRecord("q1", "first"));
            var result = pipeline.Execute(
                Request(typeof(TestRequiredMaster), typeof(TestRequiredRecord), PipelineMode.Validate),
                new ProjectTables(new[] { quest }, new ValidationIssue[0]));

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsFalse(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.MissingRequiredField));
        }

        [Test]
        public void Execute_BrokenReference()
        {
            Write(RefJsonBroken);
            var result = pipeline.Execute(
                Request(typeof(TestRefMaster), typeof(TestRefRecord), PipelineMode.Validate),
                ReferenceTables());

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.BrokenReference));
            Assert.IsNull(result.Save);
        }

        [Test]
        public void Execute_ValidReference_NoError()
        {
            Write(RefJsonValid);
            var result = pipeline.Execute(
                Request(typeof(TestRefMaster), typeof(TestRefRecord), PipelineMode.Validate),
                ReferenceTables());

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(0, result.Validation.Issues.Count);
            Assert.IsTrue(result.CanSave);
        }

        [Test]
        public void Execute_MissingTargetTable_ReferenceTargetUnavailable()
        {
            Write(RefJsonValid);
            var result = pipeline.Execute(
                Request(typeof(TestRefMaster), typeof(TestRefRecord), PipelineMode.Validate),
                ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.ReferenceTargetUnavailable));
            Assert.IsNull(result.Save);
        }

        [Test]
        public void Execute_ReplacesSameMasterType_SelfReferenceUsesImportedTable()
        {
            Write("{ \"records\": [ { \"id\": 1, \"parentId\": 0 }, { \"id\": 5, \"parentId\": 1 } ] }");
            var oldTable = Table(
                "TestNodeMaster",
                typeof(TestNodeMaster),
                typeof(TestNodeRecord),
                MasterIdKind.Int,
                new TestNodeRecord(1, 0));
            var result = pipeline.Execute(
                Request(typeof(TestNodeMaster), typeof(TestNodeRecord), PipelineMode.Validate),
                new ProjectTables(new[] { oldTable }, new ValidationIssue[0]));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(0, result.Validation.Issues.Count);
        }

        [Test]
        public void Execute_OldTableNotUsed_BrokenReference()
        {
            Write("{ \"records\": [ { \"id\": 5, \"parentId\": 9 } ] }");
            var oldTable = Table(
                "TestNodeMaster",
                typeof(TestNodeMaster),
                typeof(TestNodeRecord),
                MasterIdKind.Int,
                new TestNodeRecord(9, 0));
            var result = pipeline.Execute(
                Request(typeof(TestNodeMaster), typeof(TestNodeRecord), PipelineMode.Validate),
                new ProjectTables(new[] { oldTable }, new ValidationIssue[0]));

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.IsNotNull(FindKind(result.Validation, ValidationIssueKind.BrokenReference));
        }

        [Test]
        public void Execute_NonTargetDuplicateIds_NotReported()
        {
            Write(QuestJson);
            var duplicates = new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestItemRecord(1, "a"), new TestItemRecord(1, "b") },
                null);
            var result = pipeline.Execute(
                Request(typeof(TestQuestMaster), typeof(TestQuestRecord), PipelineMode.Validate),
                new ProjectTables(new[] { duplicates }, new ValidationIssue[0]));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsNull(FindKind(result.Validation, ValidationIssueKind.DuplicateId));
        }

        [Test]
        public void Execute_ProjectWarning_IncludedAndDoesNotBlockSave()
        {
            Write("{ \"records\": [ { \"id\": 1, \"name\": \"Potion\", \"extra\": 1 } ] }");
            var warning = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.MultipleMasterAssets,
                ProjectMasterTableCollector.ValidatorId,
                "複数です",
                "OtherMaster");
            var result = pipeline.Execute(
                ItemRequest(PipelineMode.Validate),
                new ProjectTables(new MasterTable[0], new[] { warning }));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            var unknown = IndexOfKind(result.Validation, ValidationIssueKind.UnknownField);
            var project = IndexOfKind(result.Validation, ValidationIssueKind.MultipleMasterAssets);
            Assert.GreaterOrEqual(unknown, 0);
            Assert.Greater(project, unknown);
            Assert.AreEqual("複数です", result.Validation.Issues[project].Message);
        }

        [Test]
        public void Execute_SaveNewItem_Created()
        {
            Write(ItemJson);
            var path = Asset("Item.asset");
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, result.Status);
            Assert.AreEqual(SaveOutcome.Created, result.Save.Outcome);
            Assert.AreEqual(path, result.Save.AssetPath);
            Assert.AreEqual(-1, result.Save.PreviousRecordCount);
            Assert.AreEqual(2, result.Save.NewRecordCount);
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsTrue(result.CanSave);
            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual("Potion", loaded.Records[0].Name);
        }

        [Test]
        public void Execute_SaveNewQuest_Created()
        {
            Write(QuestJson);
            var path = Asset("Quest.asset");
            var result = pipeline.Execute(
                Request(typeof(TestQuestMaster), typeof(TestQuestRecord), PipelineMode.ValidateAndSave, "Quest.asset"),
                ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, result.Status);
            Assert.AreEqual(SaveOutcome.Created, result.Save.Outcome);
            Assert.AreEqual(1, result.Save.NewRecordCount);
            Assert.AreEqual(-1, result.Save.PreviousRecordCount);
            Assert.AreEqual(0, confirm.Calls);
            var loaded = AssetDatabase.LoadAssetAtPath<TestQuestMaster>(path);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Quest_A", loaded.Records[0].Id);
            Assert.AreEqual("First", loaded.Records[0].Title);
        }

        [Test]
        public void Execute_Update_ConfirmTrue_Updated()
        {
            Write(ItemJsonOne);
            var path = Asset("Item.asset");
            var created = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);
            Assert.AreEqual(SaveOutcome.Created, created.Save.Outcome);
            Assert.AreEqual(0, confirm.Calls);

            Write(ItemJson);
            confirm.Result = true;
            var updated = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, updated.Status);
            Assert.AreEqual(SaveOutcome.Updated, updated.Save.Outcome);
            Assert.AreEqual(1, confirm.Calls);
            Assert.AreEqual(path, confirm.Path);
            Assert.AreEqual(1, confirm.Previous);
            Assert.AreEqual(2, confirm.Next);
            Assert.AreEqual(1, updated.Save.PreviousRecordCount);
            Assert.AreEqual(2, updated.Save.NewRecordCount);
            Assert.AreEqual(2, AssetDatabase.LoadAssetAtPath<TestItemMaster>(path).Count);
        }

        [Test]
        public void Execute_Update_ConfirmFalse_Cancelled()
        {
            Write(ItemJsonOne);
            var path = Asset("Item.asset");
            pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Write(ItemJson);
            confirm.Result = false;
            var cancelled = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.SaveCancelled, cancelled.Status);
            Assert.IsNull(cancelled.Save);
            Assert.IsTrue(cancelled.CanSave);
            Assert.AreEqual(1, confirm.Calls);
            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            Assert.AreEqual(1, loaded.Count);
            Assert.AreEqual("Potion", loaded.Records[0].Name);
        }

        [Test]
        public void Execute_SameContent_ConfirmTrue_Unchanged()
        {
            Write(ItemJson);
            pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            confirm.Result = true;
            var again = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, again.Status);
            Assert.AreEqual(SaveOutcome.Unchanged, again.Save.Outcome);
            Assert.AreEqual(1, confirm.Calls);
            Assert.AreEqual(2, confirm.Previous);
            Assert.AreEqual(2, confirm.Next);
        }

        [Test]
        public void Execute_DifferentType_SaveFailed_NoConfirm()
        {
            Write(QuestJson);
            var path = Asset("Mixed.asset");
            var created = pipeline.Execute(
                Request(typeof(TestQuestMaster), typeof(TestQuestRecord), PipelineMode.ValidateAndSave, "Mixed.asset"),
                ProjectTables.Empty);
            Assert.AreEqual(SaveOutcome.Created, created.Save.Outcome);

            Write(ItemJson);
            var failed = pipeline.Execute(
                Request(typeof(TestItemMaster), typeof(TestItemRecord), PipelineMode.ValidateAndSave, "Mixed.asset"),
                ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.SaveFailed, failed.Status);
            Assert.AreEqual(SaveOutcome.Failed, failed.Save.Outcome);
            Assert.AreEqual(SaveIssueKind.AssetTypeMismatch, failed.Save.Issues[0].Kind);
            Assert.AreEqual(0, confirm.Calls);
            Assert.IsTrue(failed.CanSave);
            Assert.AreEqual("Quest_A", AssetDatabase.LoadAssetAtPath<TestQuestMaster>(path).Records[0].Id);
        }

        [Test]
        public void Execute_MissingFolder_Created()
        {
            Write(ItemJson);
            var path = Asset("nested/deep/Item.asset");
            var result = pipeline.Execute(
                Request(typeof(TestItemMaster), typeof(TestItemRecord), PipelineMode.ValidateAndSave, "nested/deep/Item.asset"),
                ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, result.Status);
            Assert.AreEqual(SaveOutcome.Created, result.Save.Outcome);
            Assert.IsTrue(AssetDatabase.IsValidFolder(folder + "/nested/deep"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TestItemMaster>(path));
        }

        [Test]
        public void Execute_ValidateMode_ExistingAsset_DoesNotConfirm()
        {
            Write(ItemJson);
            var path = Asset("Item.asset");
            pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);
            Assert.AreEqual(0, confirm.Calls);

            var validated = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, validated.Status);
            Assert.IsNull(validated.Save);
            Assert.AreEqual(0, confirm.Calls);
            Assert.AreEqual(2, AssetDatabase.LoadAssetAtPath<TestItemMaster>(path).Count);
        }

        [Test]
        public void Execute_ConfirmThrows_UnexpectedError()
        {
            Write(ItemJsonOne);
            var path = Asset("Item.asset");
            pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Write(ItemJson);
            confirm.Throw = new InvalidOperationException("boom");
            PipelineResult result = null;
            Assert.DoesNotThrow(
                () => result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty));

            Assert.AreEqual(PipelineStatus.UnexpectedError, result.Status);
            StringAssert.Contains("InvalidOperationException", result.Message);
            StringAssert.Contains("boom", result.Message);
            Assert.IsNull(result.Table);
            Assert.AreSame(ValidationResult.Empty, result.Validation);
            Assert.IsNull(result.Save);
            Assert.IsFalse(result.CanSave);
            Assert.AreEqual(1, AssetDatabase.LoadAssetAtPath<TestItemMaster>(path).Count);
        }

        [Test]
        public void Execute_NullProjectTables_TreatedAsEmpty()
        {
            Write(ItemJson);
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), null);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(2, result.Table.Records.Count);
        }

        [Test]
        public void Execute_RoundTrip_LoadedAssetValidates()
        {
            Write(ItemJson);
            var path = Asset("Item.asset");
            var saved = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);
            Assert.AreEqual(PipelineStatus.Saved, saved.Status);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            var table = MasterTable.FromAsset(loaded);
            var context = new ValidationContext(new[] { table });
            var validation = new ValidationRunner(DefaultValidators.Create()).Run(context);

            Assert.IsTrue(validation.IsValid);
            Assert.AreEqual(0, validation.Issues.Count);
            Assert.AreEqual(2, table.Records.Count);
        }

        [Test]
        public void Execute_NameClash_ExcludesOtherTable()
        {
            Write("{ \"records\": [ { \"id\": 1 } ] }");
            var other = new MasterTable(
                "ClashMaster",
                typeof(ClashB),
                typeof(ClashRecordB),
                MasterIdKind.Int,
                new IMasterRecord[] { new ClashRecordB(9) },
                null);
            var projectWarning = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.MultipleMasterAssets,
                ProjectMasterTableCollector.ValidatorId,
                "先の警告",
                "OtherMaster");
            PipelineResult result = null;
            Assert.DoesNotThrow(() => result = pipeline.Execute(
                Request(typeof(ClashA), typeof(ClashRecordA), PipelineMode.Validate, "Clash.asset"),
                new ProjectTables(new[] { other }, new[] { projectWarning })));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            var warning = FindKind(result.Validation, ValidationIssueKind.AmbiguousMasterName);
            Assert.IsNotNull(warning);
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
            Assert.Greater(
                IndexOfKind(result.Validation, ValidationIssueKind.AmbiguousMasterName),
                IndexOfKind(result.Validation, ValidationIssueKind.MultipleMasterAssets));
        }

        [Test]
        public void Execute_ImportFailed_DoesNotAddNameClashWarning()
        {
            Write("{");
            var other = new MasterTable(
                "ClashMaster",
                typeof(ClashB),
                typeof(ClashRecordB),
                MasterIdKind.Int,
                new IMasterRecord[] { new ClashRecordB(9) },
                null);
            var result = pipeline.Execute(
                Request(typeof(ClashA), typeof(ClashRecordA), PipelineMode.Validate, "Clash.asset"),
                new ProjectTables(new[] { other }, new ValidationIssue[0]));

            Assert.AreEqual(PipelineStatus.ImportFailed, result.Status);
            Assert.IsNull(FindKind(result.Validation, ValidationIssueKind.AmbiguousMasterName));
            Assert.IsNotNull(result.Table);
            Assert.AreEqual(0, result.Table.Records.Count);
        }

        void Write(string json)
        {
            File.WriteAllText(jsonPath, json);
        }

        string Asset(string fileName)
        {
            return TempAssetFolder.AssetPath(folder, fileName);
        }

        PipelineRequest ItemRequest(PipelineMode mode)
        {
            return Request(typeof(TestItemMaster), typeof(TestItemRecord), mode, "Item.asset");
        }

        PipelineRequest Request(Type masterType, Type recordType, PipelineMode mode, string fileName = "Item.asset")
        {
            return new PipelineRequest(masterType, recordType, jsonPath, Asset(fileName), mode);
        }

        PipelineResult ExecuteRaw(Type masterType, Type recordType, string json, string assetPath)
        {
            return pipeline.Execute(
                new PipelineRequest(masterType, recordType, json, assetPath, PipelineMode.Validate),
                ProjectTables.Empty);
        }

        void AssertInvalid(PipelineResult result, string fragment)
        {
            Assert.AreEqual(PipelineStatus.InvalidRequest, result.Status);
            StringAssert.Contains(fragment, result.Message);
            Assert.IsNull(result.Table);
            Assert.AreSame(ValidationResult.Empty, result.Validation);
            Assert.IsFalse(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.AreEqual(0, confirm.Calls);
        }

        static ProjectTables ReferenceTables()
        {
            var items = new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestItemRecord(1, "a"), new TestItemRecord(2, "b") },
                null);
            var quests = Table(
                "TestQuestMaster",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                new TestQuestRecord("q1", "first"));
            return new ProjectTables(new[] { items, quests }, new ValidationIssue[0]);
        }

        static MasterTable Table(
            string name,
            Type masterType,
            Type recordType,
            MasterIdKind idKind,
            IMasterRecord record)
        {
            return new MasterTable(name, masterType, recordType, idKind, new[] { record }, null);
        }

        static ValidationIssue FindKind(ValidationResult validation, ValidationIssueKind kind)
        {
            var index = IndexOfKind(validation, kind);
            return index < 0 ? null : validation.Issues[index];
        }

        static int IndexOfKind(ValidationResult validation, ValidationIssueKind kind)
        {
            var issues = validation.Issues;
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Kind == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        sealed class FakeConfirm : IOverwriteConfirmation
        {
            public bool Result = true;
            public int Calls;
            public string Path;
            public int Previous;
            public int Next;
            public Exception Throw;

            public bool Confirm(string assetPath, int previousRecordCount, int newRecordCount)
            {
                Calls++;
                Path = assetPath;
                Previous = previousRecordCount;
                Next = newRecordCount;
                if (Throw != null)
                {
                    throw Throw;
                }

                return Result;
            }
        }
    }
}
