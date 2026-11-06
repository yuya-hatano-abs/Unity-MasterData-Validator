using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="DuplicateIdValidator"/> のテストです。
    /// </summary>
    public class DuplicateIdValidatorTests
    {
        const string ItemJsonDuplicateIds =
            "{\n  \"records\": [\n    { \"id\": 1, \"name\": \"A\" },\n    { \"id\": 2, \"name\": \"B\" },\n    { \"id\": 1, \"name\": \"C\" }\n  ]\n}";

        const string ItemJsonDuplicateAndTypeMismatch =
            "{\n  \"records\": [\n    { \"id\": 1, \"name\": \"A\" },\n    { \"id\": \"bad\", \"name\": \"B\" },\n    { \"id\": 1, \"name\": \"C\" }\n  ]\n}";

        readonly DuplicateIdValidator validator = new DuplicateIdValidator();

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

        static MasterTable CreateItemTable(IEnumerable<IMasterRecord> records, string assetPath = null)
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                records,
                assetPath);
        }

        static MasterTable CreateQuestTable(IEnumerable<IMasterRecord> records, string assetPath = null)
        {
            return new MasterTable(
                "TestQuestMaster",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                records,
                assetPath);
        }

        static ValidationResult RunValidator(MasterTable table)
        {
            return new DuplicateIdValidator().Validate(new ValidationContext(new[] { table }));
        }

        static ValidationResult RunValidator(MasterTable[] tables, MasterTable[] targets)
        {
            return new DuplicateIdValidator().Validate(new ValidationContext(tables, targets));
        }

        static void AssertDuplicateIssue(
            ValidationIssue issue,
            MasterTable table,
            int recordIndex,
            string recordKey,
            string firstIndexToken)
        {
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual(ValidationIssueKind.DuplicateId, issue.Kind);
            Assert.AreEqual(DuplicateIdValidator.ValidatorId, issue.ValidatorId);
            Assert.AreEqual(table.Name, issue.MasterName);
            Assert.AreEqual(table.AssetPath, issue.AssetPath);
            Assert.AreEqual(recordIndex, issue.RecordIndex);
            Assert.AreEqual(recordKey, issue.RecordKey);
            Assert.AreEqual("id", issue.FieldName);
            Assert.AreEqual(0, issue.LineNumber);
            StringAssert.Contains(table.Name, issue.Message);
            StringAssert.Contains("records[" + recordIndex + "]", issue.Message);
            StringAssert.Contains(firstIndexToken, issue.Message);
            StringAssert.Contains("'" + recordKey + "'", issue.Message);
        }

        [Test]
        public void Validate_NoDuplicates_ReturnsEmpty()
        {
            var table = CreateItemTable(new[]
            {
                new TestItemRecord(1, "A"),
                new TestItemRecord(2, "B"),
            });

            var result = validator.Validate(new ValidationContext(new[] { table }));

            Assert.AreSame(ValidationResult.Empty, result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Issues.Count);
        }

        [Test]
        public void Validate_IntIdDuplicate_ReportsOneIssue()
        {
            var table = CreateItemTable(new[]
            {
                new TestItemRecord(1, "A"),
                new TestItemRecord(2, "B"),
                new TestItemRecord(1, "C"),
            });

            var result = RunValidator(table);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 2, "1", "records[0]");
        }

        [Test]
        public void Validate_StringIdDuplicate_ReportsOneIssue()
        {
            var table = CreateQuestTable(new[]
            {
                new TestQuestRecord("A", "t1"),
                new TestQuestRecord("B", "t2"),
                new TestQuestRecord("A", "t3"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 2, "A", "records[0]");
        }

        [Test]
        public void Validate_FourSameIds_ReportsThreeIssues()
        {
            var table = CreateItemTable(new[]
            {
                new TestItemRecord(5, "a"),
                new TestItemRecord(5, "b"),
                new TestItemRecord(5, "c"),
                new TestItemRecord(5, "d"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(3, result.Issues.Count);
            Assert.AreEqual(1, result.Issues[0].RecordIndex);
            Assert.AreEqual(2, result.Issues[1].RecordIndex);
            Assert.AreEqual(3, result.Issues[2].RecordIndex);
            for (var i = 0; i < result.Issues.Count; i++)
            {
                AssertDuplicateIssue(result.Issues[i], table, result.Issues[i].RecordIndex, "5", "records[0]");
            }
        }

        [Test]
        public void Validate_MultipleDuplicateGroups_ReportsTwoIssuesInRecordIndexOrder()
        {
            var table = CreateItemTable(new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(2, "b"),
                new TestItemRecord(1, "c"),
                new TestItemRecord(2, "d"),
                new TestItemRecord(3, "e"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(2, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 2, "1", "records[0]");
            AssertDuplicateIssue(result.Issues[1], table, 3, "2", "records[1]");
        }

        [Test]
        public void Validate_CaseSensitive_DifferentCaseNoDuplicate()
        {
            var table = CreateQuestTable(new[]
            {
                new TestQuestRecord("Quest_A", "a"),
                new TestQuestRecord("quest_a", "b"),
            });

            var result = RunValidator(table);

            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_CaseSensitive_SameCaseIsDuplicate()
        {
            var table = CreateQuestTable(new[]
            {
                new TestQuestRecord("Quest_A", "a"),
                new TestQuestRecord("Quest_A", "b"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 1, "Quest_A", "records[0]");
        }

        [Test]
        public void Validate_SkipsNullRecord_ReportsDuplicateAtCorrectIndex()
        {
            var table = CreateItemTable(new IMasterRecord[]
            {
                new TestItemRecord(1, "a"),
                null,
                new TestItemRecord(1, "b"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 2, "1", "records[0]");
        }

        [Test]
        public void Validate_SkipsEmptyOrNullStringKeys()
        {
            var table = CreateQuestTable(new IMasterRecord[]
            {
                new TestQuestRecord(null, "empty-id"),
                new TestQuestRecord(string.Empty, "blank-id"),
                new TestQuestRecord("valid", "ok"),
                new TestQuestRecord("valid", "dup"),
            });

            var result = RunValidator(table);

            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], table, 3, "valid", "records[2]");
        }

        [Test]
        public void Validate_CrossTableSameKey_NoIssueWithinEachTable()
        {
            var item = CreateItemTable(new[] { new TestItemRecord(1, "i") });
            var quest = CreateQuestTable(new[] { new TestQuestRecord("1", "q") });

            var result = RunValidator(new[] { item, quest }, new[] { item, quest });

            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_CrossTableSameKey_ReportsEachTableDuplicates()
        {
            var item = CreateItemTable(new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(1, "b"),
            });
            var quest = CreateQuestTable(new[]
            {
                new TestQuestRecord("X", "a"),
                new TestQuestRecord("X", "b"),
            });

            var result = RunValidator(new[] { item, quest }, new[] { item, quest });

            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual("TestItemMaster", result.Issues[0].MasterName);
            AssertDuplicateIssue(result.Issues[0], item, 1, "1", "records[0]");
            Assert.AreEqual("TestQuestMaster", result.Issues[1].MasterName);
            AssertDuplicateIssue(result.Issues[1], quest, 1, "X", "records[0]");
        }

        [Test]
        public void Validate_OnlyTargetsTables_SkipsNonTargetDuplicates()
        {
            var withDup = CreateItemTable(new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(1, "b"),
            });
            var clean = CreateQuestTable(new[] { new TestQuestRecord("Q", "t") });

            var result = RunValidator(new[] { withDup, clean }, new[] { clean });

            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_OnlyTargetsTables_ReportsTargetDuplicates()
        {
            var withDup = CreateItemTable(new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(1, "b"),
            });
            var clean = CreateQuestTable(new[] { new TestQuestRecord("Q", "t") });

            var result = RunValidator(new[] { withDup, clean }, new[] { withDup });

            Assert.AreEqual(1, result.Issues.Count);
            AssertDuplicateIssue(result.Issues[0], withDup, 1, "1", "records[0]");
        }

        [Test]
        public void Validate_DoesNotMutateRecords()
        {
            var r0 = new TestItemRecord(1, "a");
            var r1 = new TestItemRecord(1, "b");
            var table = CreateItemTable(new IMasterRecord[] { r0, r1 });

            var countBefore = table.Records.Count;
            var keysBefore = table.Records.Select(r => r == null ? null : r.Key).ToArray();
            var refsBefore = table.Records.ToArray();

            RunValidator(table);

            Assert.AreEqual(countBefore, table.Records.Count);
            for (var i = 0; i < table.Records.Count; i++)
            {
                Assert.AreSame(refsBefore[i], table.Records[i]);
                if (table.Records[i] != null)
                {
                    Assert.AreEqual(keysBefore[i], table.Records[i].Key);
                }
            }
        }

        [Test]
        public void Validate_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
        }

        [Test]
        public void ValidationRunner_WithDefaultValidators_ReportsDuplicateId()
        {
            var table = CreateItemTable(new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(1, "b"),
            });
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table }));

            Assert.AreEqual(1, result.ErrorCount);
            Assert.AreEqual(ValidationIssueKind.DuplicateId, result.Issues[0].Kind);
            Assert.AreEqual(DuplicateIdValidator.ValidatorId, result.Issues[0].ValidatorId);
        }

        [Test]
        public void DefaultValidators_Create_ContainsDuplicateIdValidator()
        {
            var validators = DefaultValidators.Create();
            var duplicateIdCount = 0;
            for (var i = 0; i < validators.Count; i++)
            {
                if (validators[i].Id == DuplicateIdValidator.ValidatorId)
                {
                    duplicateIdCount++;
                }
            }

            Assert.AreEqual(1, duplicateIdCount);
            Assert.AreEqual(DuplicateIdValidator.ValidatorId, validators[0].Id);
            Assert.IsInstanceOf<DuplicateIdValidator>(validators[0]);
        }

        [Test]
        public void DefaultValidators_Create_ReturnsNewValidatorInstanceEachTime()
        {
            var first = DefaultValidators.Create();
            var second = DefaultValidators.Create();
            Assert.AreNotSame(first, second);
            Assert.AreNotSame(first[0], second[0]);
        }

        [Test]
        public void Validate_FromImportJson_ReportsDuplicateAfterSuccessfulImport()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonDuplicateIds);
            Assert.IsTrue(import.Succeeded);
            Assert.AreEqual(0, import.Issues.Count);

            var table = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, null);
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table }));

            Assert.AreEqual(1, result.ErrorCount);
            Assert.AreEqual(ValidationIssueKind.DuplicateId, result.Issues[0].Kind);
            Assert.AreEqual(2, result.Issues[0].RecordIndex);
        }

        [Test]
        public void Validate_FromSavedAsset_ReportsDuplicateWithAssetPath()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonDuplicateIds);
            Assert.IsTrue(import.Succeeded);
            Assert.AreEqual(0, import.Issues.Count);

            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var save = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            Assert.AreEqual(SaveOutcome.Created, save.Outcome);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            var table = MasterTable.FromAsset(loaded);
            var result = RunValidator(table);

            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(path, result.Issues[0].AssetPath);
            AssertDuplicateIssue(result.Issues[0], table, 2, "1", "records[0]");
        }

        [Test]
        public void Validate_TryGetByKeyReturnsFirstRecord_NotDuplicateIndex()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonDuplicateIds);
            var path = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });

            var loaded = AssetDatabase.LoadAssetAtPath<TestItemMaster>(path);
            Assert.IsTrue(loaded.TryGetByKey("1", out var found));
            Assert.AreEqual("A", found.Name);

            var table = MasterTable.FromAsset(loaded);
            var result = RunValidator(table);
            Assert.AreEqual(2, result.Issues[0].RecordIndex);
            Assert.AreNotSame(found, table.Records[2]);
        }

        [Test]
        public void Validate_CombinedWithImportIssues_ErrorCountsAdd()
        {
            var import = MasterJsonImporter.Import<TestItemRecord>(ItemJsonDuplicateAndTypeMismatch);
            Assert.IsTrue(import.Succeeded);
            var importErrorCount = import.Issues.Count(i => i.Severity == ImportSeverity.Error);
            Assert.Greater(importErrorCount, 0);

            var table = MasterTable.FromImport<TestItemMaster, TestItemRecord>(import, null);
            var importValidation = new ValidationResult(
                ImportIssueConverter.ConvertAll(import.Issues, table.Name, table.AssetPath));
            var duplicateValidation = new ValidationRunner(DefaultValidators.Create())
                .Run(new ValidationContext(new[] { table }));
            var combined = ValidationResult.Combine(new[] { importValidation, duplicateValidation });

            Assert.AreEqual(importValidation.ErrorCount + duplicateValidation.ErrorCount, combined.ErrorCount);
            Assert.IsTrue(combined.Issues.Any(i => i.Kind == ValidationIssueKind.TypeMismatch));
            Assert.IsTrue(combined.Issues.Any(i => i.Kind == ValidationIssueKind.DuplicateId));
        }
    }
}
