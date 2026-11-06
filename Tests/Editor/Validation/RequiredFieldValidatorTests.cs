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
    /// <see cref="RequiredFieldValidator"/> のテストです。
    /// </summary>
    public class RequiredFieldValidatorTests
    {
        readonly RequiredFieldValidator validator = new RequiredFieldValidator();

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

        static TestRequiredRecord Valid(string id = "A")
        {
            return new TestRequiredRecord(
                id,
                "Name",
                new List<int> { 1 },
                new[] { "tag" },
                "note",
                "Quest_A");
        }

        static MasterTable RequiredTable(string assetPath, params IMasterRecord[] records)
        {
            return new MasterTable(
                "TestRequiredMaster",
                typeof(TestRequiredMaster),
                typeof(TestRequiredRecord),
                MasterIdKind.String,
                records,
                assetPath);
        }

        static MasterTable QuestTable()
        {
            return new MasterTable(
                "TestQuestMaster",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                new IMasterRecord[] { new TestQuestRecord("Quest_A", "A") },
                null);
        }

        static MasterTable ItemTable()
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestItemRecord(0, "Zero") },
                null);
        }

        static MasterTable InvalidTable<TRecord>(params IMasterRecord[] records)
            where TRecord : MasterRecordBase
        {
            return new MasterTable(
                typeof(TRecord).Name,
                null,
                typeof(TRecord),
                MasterIdKind.Int,
                records,
                null);
        }

        static ValidationResult Run(params MasterTable[] tables)
        {
            return new RequiredFieldValidator().Validate(new ValidationContext(tables));
        }

        static ValidationResult Run(MasterTable[] tables, MasterTable[] targets)
        {
            return new RequiredFieldValidator().Validate(new ValidationContext(tables, targets));
        }

        static void AssertMissingField(ValidationIssue issue, MasterTable table, int recordIndex, string fieldName)
        {
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual(ValidationIssueKind.MissingRequiredField, issue.Kind);
            Assert.AreEqual(RequiredFieldValidator.ValidatorId, issue.ValidatorId);
            Assert.AreEqual(table.Name, issue.MasterName);
            Assert.AreEqual(table.AssetPath, issue.AssetPath);
            Assert.AreEqual(recordIndex, issue.RecordIndex);
            Assert.AreEqual(fieldName, issue.FieldName);
            Assert.AreEqual(0, issue.LineNumber);
            StringAssert.Contains(table.Name, issue.Message);
        }

        [Test]
        public void Validate_AllRequiredFilled_ReturnsEmpty()
        {
            var table = RequiredTable(null, Valid());
            var result = Run(table);
            Assert.AreSame(ValidationResult.Empty, result);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_RequiredString_NullEmptyAndWhitespace_AreReported()
        {
            var table = RequiredTable(
                "Assets/X/Req.asset",
                new TestRequiredRecord("A", null, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("B", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("C", "   ", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("D", "\t\n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("E", "x", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"));

            var result = Run(table);
            Assert.AreEqual(4, result.Issues.Count);
            for (var i = 0; i < 4; i++)
            {
                AssertMissingField(result.Issues[i], table, i, "name");
                Assert.AreEqual(table.Records[i].Key, result.Issues[i].RecordKey);
                StringAssert.Contains("records[" + i + "].name", result.Issues[i].Message);
            }
        }

        [Test]
        public void Validate_OptionalNote_EmptyIsNotReported()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", new List<int> { 1 }, new[] { "t" }, null, "Quest_A"),
                new TestRequiredRecord("B", "n", new List<int> { 1 }, new[] { "t" }, string.Empty, "Quest_A"));
            Assert.AreSame(ValidationResult.Empty, Run(table));
        }

        [Test]
        public void Validate_RequiredList_NullAndEmpty_AreReported()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", null, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("B", "n", new List<int>(), new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("C", "n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"));
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual("dropItemIds", result.Issues[0].FieldName);
            Assert.AreEqual("dropItemIds", result.Issues[1].FieldName);
        }

        [Test]
        public void Validate_RequiredArray_NullEmptyAndEmptyElement()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", new List<int> { 1 }, null, "n", "Quest_A"),
                new TestRequiredRecord("B", "n", new List<int> { 1 }, Array.Empty<string>(), "n", "Quest_A"),
                new TestRequiredRecord("C", "n", new List<int> { 1 }, new[] { "" }, "n", "Quest_A"));
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual("tags", result.Issues[0].FieldName);
            Assert.AreEqual(0, result.Issues[0].RecordIndex);
            Assert.AreEqual("tags", result.Issues[1].FieldName);
            Assert.AreEqual(1, result.Issues[1].RecordIndex);
        }

        [Test]
        public void Validate_StringId_NullEmptyAndWhitespace_AreReported()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord(null, "n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord(string.Empty, "n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                new TestRequiredRecord("   ", "n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                Valid("A"));
            var result = Run(table);
            Assert.AreEqual(3, result.Issues.Count);
            for (var i = 0; i < 3; i++)
            {
                AssertMissingField(result.Issues[i], table, i, "id");
                Assert.AreEqual(table.Records[i].Key, result.Issues[i].RecordKey);
                StringAssert.Contains("records[" + i + "]", result.Issues[i].Message);
                StringAssert.Contains("ID", result.Issues[i].Message);
            }
        }

        [Test]
        public void Validate_IntIdZero_IsNotReported()
        {
            Assert.AreSame(ValidationResult.Empty, Run(ItemTable()));
        }

        [Test]
        public void Validate_SameRecord_ReportsIdBeforeRequiredFields()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord(string.Empty, string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"));
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual("id", result.Issues[0].FieldName);
            Assert.AreEqual("name", result.Issues[1].FieldName);
        }

        [Test]
        public void Validate_NullRecord_ReportsOnceAndKeepsIndex()
        {
            var table = RequiredTable(null, Valid("A"), null, Valid("C"));
            var result = Run(table);
            Assert.AreEqual(1, result.Issues.Count);
            AssertMissingField(result.Issues[0], table, 1, null);
            Assert.IsNull(result.Issues[0].RecordKey);
            StringAssert.Contains("records[1]", result.Issues[0].Message);
            StringAssert.Contains("null", result.Issues[0].Message);
        }

        [Test]
        public void Validate_MultipleNullRecords_EachReported()
        {
            var table = RequiredTable(null, null, null);
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(0, result.Issues[0].RecordIndex);
            Assert.AreEqual(1, result.Issues[1].RecordIndex);
        }

        [Test]
        public void Validate_InvalidDefinitions_ReportOnceWithoutValueCheck()
        {
            AssertInvalidDefinition<TestInvalidRequired_IntFieldRecord>("value", "Int32");
            AssertInvalidDefinition<TestInvalidRequired_BoolFieldRecord>("flag", "Boolean");
            AssertInvalidDefinition<TestInvalidRequired_FloatFieldRecord>("ratio", "Single");
            AssertInvalidDefinition<TestInvalidRequired_EnumFieldRecord>("rarity", "TestRarity");
            AssertInvalidDefinition<TestInvalidRequired_NestedFieldRecord>("nested", "TestNestedValue");
            AssertInvalidDefinition<TestInvalidRequired_UnityObjectFieldRecord>("asset", "Object");
            AssertInvalidDefinition<TestInvalidRequired_NonSerializedFieldRecord>("hidden", "シリアライズ");
            AssertInvalidDefinition<TestInvalidRequired_ReadOnlyListFieldRecord>("values", "型");
        }

        void AssertInvalidDefinition<TRecord>(string fieldName, string messagePart)
            where TRecord : MasterRecordBase
        {
            var record1 = (IMasterRecord)Activator.CreateInstance(typeof(TRecord), 1);
            var record2 = (IMasterRecord)Activator.CreateInstance(typeof(TRecord), 2);
            var table = InvalidTable<TRecord>(record1, record2);
            var result = Run(table);
            Assert.AreEqual(1, result.Issues.Count, typeof(TRecord).Name);
            Assert.AreEqual(ValidationIssueKind.InvalidRequiredDefinition, result.Issues[0].Kind);
            Assert.AreEqual(-1, result.Issues[0].RecordIndex);
            Assert.IsNull(result.Issues[0].RecordKey);
            Assert.AreEqual(fieldName, result.Issues[0].FieldName);
            StringAssert.Contains(messagePart, result.Issues[0].Message);
            Assert.IsFalse(result.Issues.Any(i => i.Kind == ValidationIssueKind.MissingRequiredField));
        }

        [Test]
        public void Validate_DefinitionIssuesComeBeforeRecordIssues()
        {
            var table = new MasterTable(
                "Mixed",
                null,
                typeof(TestMixedRequiredRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestMixedRequiredRecord(1, string.Empty) },
                null);
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.InvalidRequiredDefinition, result.Issues[0].Kind);
            Assert.AreEqual("bad", result.Issues[0].FieldName);
            Assert.AreEqual(-1, result.Issues[0].RecordIndex);
            Assert.AreEqual(ValidationIssueKind.MissingRequiredField, result.Issues[1].Kind);
            Assert.AreEqual("name", result.Issues[1].FieldName);
            Assert.AreEqual(0, result.Issues[1].RecordIndex);
        }

        [Test]
        public void Validate_RecordIssues_AreOrderedByRecordIndex()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"),
                Valid("B"),
                new TestRequiredRecord("C", "n", new List<int>(), new[] { "t" }, "n", "Quest_A"));
            var result = Run(table);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(0, result.Issues[0].RecordIndex);
            Assert.AreEqual("name", result.Issues[0].FieldName);
            Assert.AreEqual(2, result.Issues[1].RecordIndex);
            Assert.AreEqual("dropItemIds", result.Issues[1].FieldName);
        }

        [Test]
        public void Validate_MultipleTargets_ReportsInTargetOrder()
        {
            var first = new MasterTable(
                "FirstRequired",
                typeof(TestRequiredMaster),
                typeof(TestRequiredRecord),
                MasterIdKind.String,
                new IMasterRecord[]
                {
                    new TestRequiredRecord("A", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A")
                },
                null);
            var second = ItemTable();
            var third = new MasterTable(
                "SecondRequired",
                null,
                typeof(TestRequiredRecord),
                MasterIdKind.String,
                new IMasterRecord[]
                {
                    new TestRequiredRecord("B", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A")
                },
                null);
            var result = Run(first, second, third);
            Assert.AreEqual("FirstRequired", result.Issues[0].MasterName);
            Assert.AreEqual("SecondRequired", result.Issues[1].MasterName);
        }

        [Test]
        public void Validate_OnlyTargetsTables_AreReported()
        {
            var dirty = RequiredTable(
                null,
                new TestRequiredRecord("A", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"));
            var clean = ItemTable();
            var result = Run(new[] { dirty, clean }, new[] { clean });
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_SameRecordType_ReportsDefinitionPerTable()
        {
            var first = InvalidTable<TestInvalidRequired_IntFieldRecord>(
                new TestInvalidRequired_IntFieldRecord(1));
            var second = new MasterTable(
                "OtherIntRequired",
                null,
                typeof(TestInvalidRequired_IntFieldRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestInvalidRequired_IntFieldRecord(2) },
                null);
            var result = Run(first, second);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(typeof(TestInvalidRequired_IntFieldRecord).Name, result.Issues[0].MasterName);
            Assert.AreEqual("OtherIntRequired", result.Issues[1].MasterName);
        }

        [Test]
        public void Validate_DoesNotMutateRecords()
        {
            var record = Valid();
            var table = RequiredTable(null, record);
            Run(table);
            Assert.AreEqual(1, table.Records.Count);
            Assert.AreSame(record, table.Records[0]);
            Assert.AreEqual("A", table.Records[0].Key);
        }

        [Test]
        public void Validate_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
        }

        [Test]
        public void Validate_RequiredAndRef_EmptyQuestId_ReportsRequiredOnly()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", new List<int> { 1 }, new[] { "t" }, "n", string.Empty));
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table, QuestTable() }));
            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ValidationIssueKind.MissingRequiredField));
            Assert.AreEqual(0, result.Issues.Count(i => i.Kind == ValidationIssueKind.BrokenReference));
            Assert.AreEqual("questId", result.Issues.First(i => i.Kind == ValidationIssueKind.MissingRequiredField).FieldName);
        }

        [Test]
        public void Validate_RequiredAndRef_MissingTarget_ReportsBrokenReferenceOnly()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", new List<int> { 1 }, new[] { "t" }, "n", "Missing"));
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table, QuestTable() }));
            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ValidationIssueKind.BrokenReference));
            Assert.AreEqual(0, result.Issues.Count(i => i.Kind == ValidationIssueKind.MissingRequiredField));
        }

        [Test]
        public void Validate_RequiredAndRef_WhitespaceQuestId_ReportsBothRequiredAndBrokenReference()
        {
            // 空白だけの questId は、必須チェックでは空、参照切れチェックでは非空の Key として扱う。
            // 同じフィールドに MissingRequiredField と BrokenReference の両方が出るのは、意図した挙動です。
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", "n", new List<int> { 1 }, new[] { "t" }, "n", "   "));
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table, QuestTable() }));
            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ValidationIssueKind.MissingRequiredField && i.FieldName == "questId"));
            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ValidationIssueKind.BrokenReference && i.FieldName == "questId"));
        }

        [Test]
        public void DefaultValidators_Create_ContainsRequiredFieldAfterBrokenReference()
        {
            var first = DefaultValidators.Create();
            var second = DefaultValidators.Create();
            var duplicateIndex = -1;
            var brokenIndex = -1;
            var requiredIndex = -1;
            for (var i = 0; i < first.Count; i++)
            {
                if (first[i].Id == DuplicateIdValidator.ValidatorId)
                {
                    duplicateIndex = i;
                }
                else if (first[i].Id == BrokenReferenceValidator.ValidatorId)
                {
                    brokenIndex = i;
                }
                else if (first[i].Id == RequiredFieldValidator.ValidatorId)
                {
                    requiredIndex = i;
                }
            }

            Assert.AreEqual(1, first.Count(v => v.Id == DuplicateIdValidator.ValidatorId));
            Assert.AreEqual(1, first.Count(v => v.Id == BrokenReferenceValidator.ValidatorId));
            Assert.AreEqual(1, first.Count(v => v.Id == RequiredFieldValidator.ValidatorId));
            Assert.Greater(brokenIndex, duplicateIndex);
            Assert.Greater(requiredIndex, brokenIndex);
            Assert.AreNotSame(first, second);
            Assert.AreNotSame(first[requiredIndex], second[requiredIndex]);
        }

        [Test]
        public void ValidationRunner_ReportsDuplicateBrokenAndRequired()
        {
            var table = RequiredTable(
                null,
                new TestRequiredRecord("A", string.Empty, new List<int> { 1 }, new[] { "t" }, "n", "Missing"),
                new TestRequiredRecord("A", "n", new List<int> { 1 }, new[] { "t" }, "n", "Quest_A"));
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table, QuestTable() }));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ValidationIssueKind.DuplicateId));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ValidationIssueKind.BrokenReference));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ValidationIssueKind.MissingRequiredField));
        }

        [Test]
        public void Validate_FromImportJson_ReportsEmptyNameListAndId()
        {
            const string json =
                "{ \"records\": [" +
                "{ \"id\": \"\", \"name\": \"\", \"dropItemIds\": [], \"tags\": [\"t\"], \"note\": \"n\", \"questId\": \"Quest_A\" }" +
                "] }";
            var import = MasterJsonImporter.Import<TestRequiredRecord>(json);
            Assert.IsTrue(import.Succeeded);
            Assert.IsFalse(import.Issues.Any(i => i.Severity == ImportSeverity.Error));

            var table = MasterTable.FromImport<TestRequiredMaster, TestRequiredRecord>(import, null);
            var result = Run(table);
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "id"));
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "name"));
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "dropItemIds"));
        }

        [Test]
        public void Validate_FromImportJson_OmittedKeys_AreReportedAsEmpty()
        {
            const string json = "{ \"records\": [ { \"id\": \"A\" } ] }";
            var import = MasterJsonImporter.Import<TestRequiredRecord>(json);
            Assert.IsTrue(import.Succeeded);
            Assert.IsTrue(import.Issues.Any(i => i.Kind == ImportIssueKind.MissingField && i.FieldName == "name"));

            var table = MasterTable.FromImport<TestRequiredMaster, TestRequiredRecord>(import, null);
            var result = Run(table);
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "name"));
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "dropItemIds"));
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "tags"));
            Assert.IsTrue(result.Issues.Any(i => i.FieldName == "questId"));
        }

        [Test]
        public void Validate_FromImportJson_NullName_HasNoMissingFieldWarning()
        {
            const string json =
                "{ \"records\": [" +
                "{ \"id\": \"A\", \"name\": null, \"dropItemIds\": [1], \"tags\": [\"t\"], \"note\": \"n\", \"questId\": \"Quest_A\" }" +
                "] }";
            var import = MasterJsonImporter.Import<TestRequiredRecord>(json);
            Assert.IsTrue(import.Succeeded);
            Assert.IsFalse(import.Issues.Any(i => i.Kind == ImportIssueKind.MissingField && i.FieldName == "name"));

            var table = MasterTable.FromImport<TestRequiredMaster, TestRequiredRecord>(import, null);
            var result = Run(table);
            Assert.AreEqual(1, result.Issues.Count(i => i.FieldName == "name" && i.Kind == ValidationIssueKind.MissingRequiredField));
        }

        [Test]
        public void Validate_FromSavedAsset_ReportsWithAssetPath()
        {
            const string json =
                "{ \"records\": [" +
                "{ \"id\": \"A\", \"name\": \"\", \"dropItemIds\": [1], \"tags\": [\"t\"], \"note\": \"n\", \"questId\": \"Quest_A\" }" +
                "] }";
            var import = MasterJsonImporter.Import<TestRequiredRecord>(json);
            var path = TempAssetFolder.AssetPath(tempFolder, "TestRequiredMaster.asset");
            var save = MasterAssetSaver.Save<TestRequiredMaster, TestRequiredRecord>(
                import,
                path,
                new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            Assert.AreEqual(SaveOutcome.Created, save.Outcome);

            var loaded = AssetDatabase.LoadAssetAtPath<TestRequiredMaster>(path);
            var table = MasterTable.FromAsset(loaded);
            var result = Run(table);
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("name", result.Issues[0].FieldName);
            Assert.AreEqual(path, result.Issues[0].AssetPath);
        }

        [Test]
        public void Validate_CombinedWithImportIssues_ErrorCountsAdd()
        {
            const string json =
                "{ \"records\": [" +
                "{ \"id\": \"A\", \"name\": 1, \"dropItemIds\": [], \"tags\": [\"t\"], \"note\": \"n\", \"questId\": \"Quest_A\" }" +
                "] }";
            var import = MasterJsonImporter.Import<TestRequiredRecord>(json);
            Assert.IsTrue(import.Succeeded);
            var importErrorCount = import.Issues.Count(i => i.Severity == ImportSeverity.Error);
            Assert.Greater(importErrorCount, 0);

            var table = MasterTable.FromImport<TestRequiredMaster, TestRequiredRecord>(import, null);
            var importValidation = new ValidationResult(
                ImportIssueConverter.ConvertAll(import.Issues, table.Name, table.AssetPath));
            var requiredValidation = Run(table);
            var combined = ValidationResult.Combine(new[] { importValidation, requiredValidation });
            Assert.AreEqual(importValidation.ErrorCount + requiredValidation.ErrorCount, combined.ErrorCount);
            Assert.IsTrue(combined.Issues.Any(i => i.Kind == ValidationIssueKind.TypeMismatch));
            Assert.IsTrue(combined.Issues.Any(i => i.Kind == ValidationIssueKind.MissingRequiredField));
        }
    }
}
