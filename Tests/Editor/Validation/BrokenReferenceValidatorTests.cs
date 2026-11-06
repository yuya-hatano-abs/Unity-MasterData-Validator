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
    /// <see cref="BrokenReferenceValidator"/> のテストです。
    /// </summary>
    public class BrokenReferenceValidatorTests
    {
        const string ItemJson = "{ \"records\": [ { \"id\": 1, \"name\": \"A\" }, { \"id\": 2, \"name\": \"B\" } ] }";
        const string RefJsonBrokenItem =
            "{ \"records\": [ { \"id\": 10, \"itemId\": 999, \"optionalItemId\": 0, \"questId\": \"Quest_A\", \"itemIds\": [], \"questIds\": [], \"plainNumber\": 0 } ] }";

        readonly BrokenReferenceValidator validator = new BrokenReferenceValidator();

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

        static MasterTable ItemTable(params IMasterRecord[] records)
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                records,
                null);
        }

        static MasterTable QuestTable(params IMasterRecord[] records)
        {
            return new MasterTable(
                "TestQuestMaster",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                records,
                null);
        }

        static MasterTable RefTable(string assetPath, params IMasterRecord[] records)
        {
            return new MasterTable(
                "TestRefMaster",
                typeof(TestRefMaster),
                typeof(TestRefRecord),
                MasterIdKind.Int,
                records,
                assetPath);
        }

        static MasterTable NodeTable(params IMasterRecord[] records)
        {
            return new MasterTable(
                "TestNodeMaster",
                typeof(TestNodeMaster),
                typeof(TestNodeRecord),
                MasterIdKind.Int,
                records,
                null);
        }

        static MasterTable DefaultItems()
        {
            return ItemTable(new TestItemRecord(1, "A"), new TestItemRecord(2, "B"));
        }

        static MasterTable DefaultQuests()
        {
            return QuestTable(new TestQuestRecord("Quest_A", "A"), new TestQuestRecord("Quest_B", "B"));
        }

        static TestRefRecord CreateRef(
            int id,
            int itemId = 1,
            int optionalItemId = 0,
            string questId = "Quest_A",
            List<int> itemIds = null,
            string[] questIds = null,
            int plainNumber = 0)
        {
            return new TestRefRecord(id, itemId, optionalItemId, questId, itemIds, questIds, plainNumber);
        }

        static ValidationResult Run(params MasterTable[] tables)
        {
            return new BrokenReferenceValidator().Validate(new ValidationContext(tables));
        }

        static ValidationResult Run(MasterTable[] tables, MasterTable[] targets)
        {
            return new BrokenReferenceValidator().Validate(new ValidationContext(tables, targets));
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

        static void AssertBroken(
            ValidationIssue issue,
            MasterTable table,
            int recordIndex,
            string recordKey,
            string fieldName,
            string missingId,
            string targetMasterName)
        {
            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual(ValidationIssueKind.BrokenReference, issue.Kind);
            Assert.AreEqual(BrokenReferenceValidator.ValidatorId, issue.ValidatorId);
            Assert.AreEqual(table.Name, issue.MasterName);
            Assert.AreEqual(table.AssetPath, issue.AssetPath);
            Assert.AreEqual(recordIndex, issue.RecordIndex);
            Assert.AreEqual(recordKey, issue.RecordKey);
            Assert.AreEqual(fieldName, issue.FieldName);
            Assert.AreEqual(0, issue.LineNumber);
            StringAssert.Contains(table.Name, issue.Message);
            StringAssert.Contains("records[" + recordIndex + "]." + fieldName, issue.Message);
            StringAssert.Contains("'" + missingId + "'", issue.Message);
            StringAssert.Contains(targetMasterName, issue.Message);
        }

        [Test]
        public void Validate_AllReferencesValid_ReturnsEmpty()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Quest_A", new List<int> { 1, 2 }, new[] { "Quest_B" }, 999));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Validate_IntBrokenReference_ReportsOneIssue()
        {
            var table = RefTable("Assets/X/Ref.asset", CreateRef(3, 999));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            AssertBroken(result.Issues[0], table, 0, "3", "itemId", "999", "TestItemMaster");
        }

        [Test]
        public void Validate_StringBrokenReference_ReportsOneIssue()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Missing"));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            AssertBroken(result.Issues[0], table, 0, "1", "questId", "Missing", "TestQuestMaster");
        }

        [Test]
        public void Validate_CollectionElement_ReportsIndexedFieldName()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Quest_A", new List<int> { 1, 999, 2 }, null));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("itemIds[1]", result.Issues[0].FieldName);
            StringAssert.Contains("records[0].itemIds[1]", result.Issues[0].Message);
        }

        [Test]
        public void Validate_StringArrayElement_ReportsIndexedFieldName()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Quest_A", null, new[] { "Missing" }));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("questIds[0]", result.Issues[0].FieldName);
        }

        [Test]
        public void Validate_OptionalNoneValueZero_IsSkipped()
        {
            var table = RefTable(null, CreateRef(1, 1, 0));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_OptionalNoneValueMissing_IsReported()
        {
            var table = RefTable(null, CreateRef(1, 1, 5));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("optionalItemId", result.Issues[0].FieldName);
            StringAssert.Contains("'5'", result.Issues[0].Message);
            StringAssert.DoesNotContain("NoneValue", result.Issues[0].Message);
        }

        [Test]
        public void Validate_ItemIdZeroWithoutNoneValue_IsReported()
        {
            var table = RefTable(null, CreateRef(1, 0));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            AssertBroken(result.Issues[0], table, 0, "1", "itemId", "0", "TestItemMaster");
        }

        [Test]
        public void Validate_ItemIdZeroWhenTargetHasZero_IsValid()
        {
            var items = ItemTable(new TestItemRecord(0, "Zero"), new TestItemRecord(1, "A"));
            var table = RefTable(null, CreateRef(1, 0));
            var result = Run(table, items, DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_ZeroBrokenReference_MessageMentionsNoneValue()
        {
            var table = RefTable(null, CreateRef(1, 0));
            var result = Run(table, DefaultItems(), DefaultQuests());
            StringAssert.Contains("NoneValue", result.Issues[0].Message);
        }

        [Test]
        public void Validate_NonZeroBrokenReference_MessageDoesNotMentionNoneValue()
        {
            var table = RefTable(null, CreateRef(1, 999));
            var result = Run(table, DefaultItems(), DefaultQuests());
            StringAssert.DoesNotContain("NoneValue", result.Issues[0].Message);
        }

        [Test]
        public void Validate_NullOrEmptyQuestId_IsSkipped()
        {
            var nullId = RefTable(null, CreateRef(1, 1, 0, null));
            var emptyId = RefTable(null, CreateRef(2, 1, 0, string.Empty));
            Assert.AreSame(ValidationResult.Empty, Run(nullId, DefaultItems(), DefaultQuests()));
            Assert.AreSame(ValidationResult.Empty, Run(emptyId, DefaultItems(), DefaultQuests()));
        }

        [Test]
        public void Validate_WhitespaceQuestId_IsReported()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, " "));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("questId", result.Issues[0].FieldName);
        }

        [Test]
        public void Validate_NullCollection_IsSkipped()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Quest_A", null, null));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_NullAndEmptyCollectionElements_KeepIndex()
        {
            var table = RefTable(
                null,
                CreateRef(1, 1, 0, "Quest_A", null, new[] { null, string.Empty, "Missing" }));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("questIds[2]", result.Issues[0].FieldName);
        }

        [Test]
        public void Validate_StringId_IsCaseSensitive()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "quest_a"));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            StringAssert.Contains("'quest_a'", result.Issues[0].Message);
        }

        [Test]
        public void Validate_PlainNumber_IsNotChecked()
        {
            var table = RefTable(null, CreateRef(1, 1, 0, "Quest_A", null, null, 999));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_DuplicateKeysInTarget_StillResolves()
        {
            var items = ItemTable(new TestItemRecord(1, "first"), new TestItemRecord(1, "second"));
            var table = RefTable(null, CreateRef(1, 1));
            var result = Run(table, items, DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_MissingTargetTable_ReportsUnavailablePerField()
        {
            var table = RefTable(
                null,
                CreateRef(1, 999, 5, "Missing", new List<int> { 999 }, new[] { "Missing" }),
                CreateRef(2, 1, 0, "Missing", new List<int> { 1 }, new[] { "Missing" }));
            var result = Run(table, DefaultQuests());

            var unavailable = result.Issues.Where(i => i.Kind == ValidationIssueKind.ReferenceTargetUnavailable).ToArray();
            Assert.AreEqual(3, unavailable.Length);
            CollectionAssert.AreEqual(new[] { "itemId", "optionalItemId", "itemIds" }, unavailable.Select(i => i.FieldName).ToArray());
            for (var i = 0; i < unavailable.Length; i++)
            {
                Assert.AreEqual(-1, unavailable[i].RecordIndex);
                Assert.IsNull(unavailable[i].RecordKey);
                StringAssert.Contains("TestItemMaster", unavailable[i].Message);
            }

            Assert.IsFalse(result.Issues.Any(i => i.Kind == ValidationIssueKind.BrokenReference && i.FieldName.StartsWith("item")));
            Assert.AreEqual(2, result.Issues.Count(i => i.Kind == ValidationIssueKind.BrokenReference && i.FieldName == "questId"));
        }

        [Test]
        public void Validate_TargetsOnly_UsesTablesForResolution()
        {
            var items = DefaultItems();
            var quests = DefaultQuests();
            var source = RefTable(null, CreateRef(1, 999));
            var result = Run(new[] { source, items, quests }, new[] { source });
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.BrokenReference, result.Issues[0].Kind);
        }

        [Test]
        public void Validate_SourceNotInTargets_DoesNotReport()
        {
            var items = DefaultItems();
            var quests = DefaultQuests();
            var source = RefTable(null, CreateRef(1, 999));
            var result = Run(new[] { source, items, quests }, new[] { items });
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_SelfReference_ValidAndBrokenAndNoneValue()
        {
            var valid = NodeTable(new TestNodeRecord(1, 0), new TestNodeRecord(2, 1));
            Assert.AreSame(ValidationResult.Empty, Run(valid));

            var broken = NodeTable(new TestNodeRecord(1, 0), new TestNodeRecord(2, 99));
            var result = Run(broken);
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("parentId", result.Issues[0].FieldName);
            StringAssert.Contains("'99'", result.Issues[0].Message);
        }

        [Test]
        public void Validate_InvalidDefinitions_ReportOnceWithoutBrokenReference()
        {
            AssertInvalidDefinition<TestInvalidRef_FloatFieldRecord>("value", "型");
            AssertInvalidDefinition<TestInvalidRef_NonMasterTargetRecord>("itemId", "MasterDataBase");
            AssertInvalidDefinition<TestInvalidRef_AbstractTargetRecord>("itemId", "abstract");
            AssertInvalidDefinition<TestInvalidRef_NullTargetRecord>("itemId", "null");
            AssertInvalidDefinition<TestInvalidRef_StringNoneValueRecord>("questId", "NoneValue", DefaultQuests());
            AssertInvalidDefinition<TestInvalidRef_NonSerializedFieldRecord>("itemId", "シリアライズ");

            var intToString = InvalidTable<TestInvalidRef_IntToStringMasterRecord>(
                new TestInvalidRef_IntToStringMasterRecord(1),
                new TestInvalidRef_IntToStringMasterRecord(2));
            var intToStringResult = Run(intToString, DefaultQuests());
            Assert.AreEqual(1, intToStringResult.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.InvalidReferenceDefinition, intToStringResult.Issues[0].Kind);
            StringAssert.Contains("ID 種類", intToStringResult.Issues[0].Message);
            Assert.IsFalse(intToStringResult.Issues.Any(i => i.Kind == ValidationIssueKind.BrokenReference));

            var stringToInt = InvalidTable<TestInvalidRef_StringToIntMasterRecord>(
                new TestInvalidRef_StringToIntMasterRecord(1),
                new TestInvalidRef_StringToIntMasterRecord(2));
            var stringToIntResult = Run(stringToInt, DefaultItems());
            Assert.AreEqual(1, stringToIntResult.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.InvalidReferenceDefinition, stringToIntResult.Issues[0].Kind);
            StringAssert.Contains("ID 種類", stringToIntResult.Issues[0].Message);
        }

        void AssertInvalidDefinition<TRecord>(string fieldName, string messagePart, params MasterTable[] extraTables)
            where TRecord : MasterRecordBase
        {
            var record1 = (IMasterRecord)Activator.CreateInstance(typeof(TRecord), 1);
            var record2 = (IMasterRecord)Activator.CreateInstance(typeof(TRecord), 2);
            var table = InvalidTable<TRecord>(record1, record2);
            var tables = new List<MasterTable> { table };
            tables.AddRange(extraTables);
            var result = Run(tables.ToArray());
            Assert.AreEqual(1, result.Issues.Count, typeof(TRecord).Name);
            Assert.AreEqual(ValidationIssueKind.InvalidReferenceDefinition, result.Issues[0].Kind);
            Assert.AreEqual(fieldName, result.Issues[0].FieldName);
            Assert.AreEqual(-1, result.Issues[0].RecordIndex);
            Assert.IsNull(result.Issues[0].RecordKey);
            StringAssert.Contains(messagePart, result.Issues[0].Message);
            Assert.IsFalse(result.Issues.Any(i => i.Kind == ValidationIssueKind.BrokenReference));
        }

        [Test]
        public void Validate_MultipleProblemsOnOneField_ReportsFirstOnly()
        {
            var table = InvalidTable<TestInvalidRef_InvalidTypeAndStringNoneValueRecord>(
                new TestInvalidRef_InvalidTypeAndStringNoneValueRecord(1),
                new TestInvalidRef_InvalidTypeAndStringNoneValueRecord(2));
            var result = Run(table, DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.InvalidReferenceDefinition, result.Issues[0].Kind);
            StringAssert.Contains("型", result.Issues[0].Message);
            StringAssert.DoesNotContain("string 系のフィールドですが", result.Issues[0].Message);
        }

        [Test]
        public void Validate_DefinitionIssuesComeBeforeRecordIssues()
        {
            var table = new MasterTable(
                "Mixed",
                null,
                typeof(TestMixedDefAndRefRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestMixedDefAndRefRecord(1, 999) },
                null);
            var result = Run(table, DefaultItems());
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.InvalidReferenceDefinition, result.Issues[0].Kind);
            Assert.AreEqual("bad", result.Issues[0].FieldName);
            Assert.AreEqual(-1, result.Issues[0].RecordIndex);
            Assert.AreEqual(ValidationIssueKind.BrokenReference, result.Issues[1].Kind);
            Assert.AreEqual("itemId", result.Issues[1].FieldName);
            Assert.AreEqual(0, result.Issues[1].RecordIndex);
        }

        [Test]
        public void Validate_RecordIssues_AreOrderedByIndexThenFieldThenElement()
        {
            var table = RefTable(
                null,
                CreateRef(1, 1, 0, "Quest_A", new List<int> { 1, 999 }, new[] { "MissingA", "MissingB" }),
                CreateRef(2, 999, 0, "Quest_A", new List<int> { 888 }, null));
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(5, result.Issues.Count);
            Assert.AreEqual(0, result.Issues[0].RecordIndex);
            Assert.AreEqual("itemIds[1]", result.Issues[0].FieldName);
            Assert.AreEqual(0, result.Issues[1].RecordIndex);
            Assert.AreEqual("questIds[0]", result.Issues[1].FieldName);
            Assert.AreEqual(0, result.Issues[2].RecordIndex);
            Assert.AreEqual("questIds[1]", result.Issues[2].FieldName);
            Assert.AreEqual(1, result.Issues[3].RecordIndex);
            Assert.AreEqual("itemId", result.Issues[3].FieldName);
            Assert.AreEqual(1, result.Issues[4].RecordIndex);
            Assert.AreEqual("itemIds[0]", result.Issues[4].FieldName);
        }

        [Test]
        public void Validate_MultipleTargets_ReportsInTargetOrder()
        {
            var refTable = RefTable(null, CreateRef(1, 999));
            var nodeTable = NodeTable(new TestNodeRecord(1, 99));
            var result = Run(refTable, nodeTable, DefaultItems(), DefaultQuests());
            Assert.AreEqual("TestRefMaster", result.Issues[0].MasterName);
            Assert.AreEqual("TestNodeMaster", result.Issues[1].MasterName);
        }

        [Test]
        public void Validate_NullRecord_KeepsIndex()
        {
            var table = RefTable(
                null,
                new IMasterRecord[] { CreateRef(1, 1), null, CreateRef(3, 999) });
            var result = Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(2, result.Issues[0].RecordIndex);
        }

        [Test]
        public void Validate_DoesNotMutateRecords()
        {
            var record = CreateRef(1, 999);
            var table = RefTable(null, record);
            var before = table.Records.ToArray();
            Run(table, DefaultItems(), DefaultQuests());
            Assert.AreEqual(before.Length, table.Records.Count);
            Assert.AreSame(record, table.Records[0]);
            Assert.AreEqual("1", table.Records[0].Key);
        }

        [Test]
        public void Validate_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
        }

        [Test]
        public void ValidationRunner_ReportsDuplicateIdThenBrokenReference()
        {
            var table = RefTable(null, CreateRef(1, 999), CreateRef(1, 1));
            var runner = new ValidationRunner(DefaultValidators.Create());
            var result = runner.Run(new ValidationContext(new[] { table, DefaultItems(), DefaultQuests() }));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ValidationIssueKind.DuplicateId));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ValidationIssueKind.BrokenReference));
            var firstDup = result.Issues.ToList().FindIndex(i => i.Kind == ValidationIssueKind.DuplicateId);
            var firstBroken = result.Issues.ToList().FindIndex(i => i.Kind == ValidationIssueKind.BrokenReference);
            Assert.Less(firstDup, firstBroken);
        }

        [Test]
        public void DefaultValidators_Create_ContainsBrokenReferenceAfterDuplicateId()
        {
            var first = DefaultValidators.Create();
            var second = DefaultValidators.Create();
            var duplicateIdCount = 0;
            var brokenCount = 0;
            var duplicateIndex = -1;
            var brokenIndex = -1;
            for (var i = 0; i < first.Count; i++)
            {
                if (first[i].Id == DuplicateIdValidator.ValidatorId)
                {
                    duplicateIdCount++;
                    duplicateIndex = i;
                }
                else if (first[i].Id == BrokenReferenceValidator.ValidatorId)
                {
                    brokenCount++;
                    brokenIndex = i;
                }
            }

            Assert.AreEqual(1, duplicateIdCount);
            Assert.AreEqual(1, brokenCount);
            Assert.Greater(brokenIndex, duplicateIndex);
            Assert.AreNotSame(first, second);
            Assert.AreNotSame(first[brokenIndex], second[brokenIndex]);
        }

        [Test]
        public void Validate_FromImportJson_ReportsBrokenReference()
        {
            var items = MasterJsonImporter.Import<TestItemRecord>(ItemJson);
            var refs = MasterJsonImporter.Import<TestRefRecord>(RefJsonBrokenItem);
            Assert.IsTrue(items.Succeeded);
            Assert.AreEqual(0, items.Issues.Count);
            Assert.IsTrue(refs.Succeeded);
            Assert.AreEqual(0, refs.Issues.Count);

            var itemTable = MasterTable.FromImport<TestItemMaster, TestItemRecord>(items, null);
            var refTable = MasterTable.FromImport<TestRefMaster, TestRefRecord>(refs, null);
            var result = Run(refTable, itemTable, DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual("TestRefMaster", result.Issues[0].MasterName);
            Assert.AreEqual(0, result.Issues[0].RecordIndex);
            Assert.AreEqual("itemId", result.Issues[0].FieldName);
        }

        [Test]
        public void Validate_FromSavedAsset_ReportsBrokenReferenceWithAssetPath()
        {
            var items = MasterJsonImporter.Import<TestItemRecord>(ItemJson);
            var refs = MasterJsonImporter.Import<TestRefRecord>(RefJsonBrokenItem);
            var itemPath = TempAssetFolder.AssetPath(tempFolder, "TestItemMaster.asset");
            var refPath = TempAssetFolder.AssetPath(tempFolder, "TestRefMaster.asset");
            var itemSave = MasterAssetSaver.Save<TestItemMaster, TestItemRecord>(
                items, itemPath, new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            var refSave = MasterAssetSaver.Save<TestRefMaster, TestRefRecord>(
                refs, refPath, new MasterAssetSaveOptions { ExistingAsset = OverwritePolicy.Overwrite });
            Assert.AreEqual(SaveOutcome.Created, itemSave.Outcome);
            Assert.AreEqual(SaveOutcome.Created, refSave.Outcome);

            var itemAsset = AssetDatabase.LoadAssetAtPath<TestItemMaster>(itemPath);
            var refAsset = AssetDatabase.LoadAssetAtPath<TestRefMaster>(refPath);
            var result = Run(MasterTable.FromAsset(refAsset), MasterTable.FromAsset(itemAsset), DefaultQuests());
            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(refPath, result.Issues[0].AssetPath);
        }

        [Test]
        public void Validate_ResolvesTargetByMasterTypeNotName()
        {
            var items = new MasterTable(
                "RenamedItems",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { new TestItemRecord(1, "A") },
                null);
            var table = RefTable(null, CreateRef(1, 1));
            var result = Run(table, items, DefaultQuests());
            Assert.AreSame(ValidationResult.Empty, result);
        }

        [Test]
        public void Validate_SameRecordTypeOnTwoTables_ReportsPerTable()
        {
            var items = DefaultItems();
            var first = new MasterTable(
                "FirstRef",
                typeof(TestRefMaster),
                typeof(TestRefRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { CreateRef(1, 1) },
                null);
            var second = new MasterTable(
                "SecondRef",
                null,
                typeof(TestRefRecord),
                MasterIdKind.Int,
                new IMasterRecord[] { CreateRef(1, 1) },
                null);
            var result = Run(first, second);
            var unavailable = result.Issues.Where(i => i.Kind == ValidationIssueKind.ReferenceTargetUnavailable).ToArray();
            Assert.IsTrue(unavailable.Any(i => i.MasterName == "FirstRef"));
            Assert.IsTrue(unavailable.Any(i => i.MasterName == "SecondRef"));
        }
    }
}
