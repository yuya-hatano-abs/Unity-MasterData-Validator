using System;
using System.IO;
using System.Linq;
using System.Text;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterJsonImporter"/> のテストです。
    /// </summary>
    public class MasterJsonImporterTests
    {
        [Test]
        public void Import_TestItemRecord_SuccessWithNoIssues()
        {
            const string json = @"
{
  ""records"": [
    { ""id"": 1, ""name"": ""Potion"" },
    { ""id"": 2, ""name"": ""Ether"" }
  ]
}";

            var result = MasterJsonImporter.Import<TestItemRecord>(json);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(0, result.Issues.Count);
            Assert.AreEqual(2, result.Records.Count);
            Assert.AreEqual(1, result.Records[0].Id);
            Assert.AreEqual("Potion", result.Records[0].Name);
        }

        [Test]
        public void Import_TestQuestRecord_SuccessWithNoIssues()
        {
            const string json = "{ \"records\": [ { \"id\": \"Quest_A\", \"title\": \"First\" } ] }";

            var result = MasterJsonImporter.Import<TestQuestRecord>(json);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.HasErrors);
            Assert.AreEqual(0, result.Issues.Count);
            Assert.AreEqual("Quest_A", result.Records[0].Id);
            Assert.AreEqual("First", result.Records[0].Title);
        }

        [Test]
        public void Import_TestAllTypesRecord_SetsAllSupportedFields()
        {
            const string json = @"
{
  ""records"": [
    {
      ""id"": 10,
      ""count"": 3,
      ""bigCount"": 9223372036854775807,
      ""ratio"": 1.5,
      ""precise"": 2.25,
      ""enabled"": true,
      ""label"": ""lbl"",
      ""rarity"": ""Rare"",
      ""options"": 5,
      ""tags"": [ ""a"", ""b"" ],
      ""values"": [ 1, 2, 3 ],
      ""nested"": { ""value"": 1 }
    }
  ]
}";

            var result = MasterJsonImporter.Import<TestAllTypesRecord>(json);

            Assert.IsTrue(result.Succeeded);
            var record = result.Records[0];
            Assert.AreEqual(10, record.Id);
            Assert.AreEqual(3, record.Count);
            Assert.AreEqual(9223372036854775807L, record.BigCount);
            Assert.AreEqual(1.5f, record.Ratio);
            Assert.AreEqual(2.25d, record.Precise);
            Assert.IsTrue(record.Enabled);
            Assert.AreEqual("lbl", record.Label);
            Assert.AreEqual(TestRarity.Rare, record.Rarity);
            Assert.AreEqual(TestFlagsOption.A | TestFlagsOption.C, record.Options);
            CollectionAssert.AreEqual(new[] { "a", "b" }, record.Tags);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, record.Values);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.UnsupportedFieldType && i.FieldName == "nested"));
        }

        [Test]
        public void Import_EmptyRecordsArray_SucceedsWithZeroRecords()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [] }");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Records.Count);
            Assert.AreEqual(0, result.Issues.Count);
        }

        [Test]
        public void Import_InvalidJsonSyntax_ReturnsInvalidJson()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ ");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ImportIssueKind.InvalidJson));
            Assert.Greater(result.Issues[0].LineNumber, 0);
        }

        [Test]
        public void Import_TrailingExtraToken_ReturnsInvalidJson()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [] } 123");

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.InvalidJson));
        }

        [Test]
        public void Import_DuplicatePropertyInRecord_ReturnsInvalidJson()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ { \"id\": 1, \"id\": 2 } ] }");

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.InvalidJson));
        }

        [Test]
        public void Import_RootArray_ReturnsMissingRecordsArray()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("[ { \"id\": 1 } ]");

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.MissingRecordsArray));
        }

        [Test]
        public void Import_RecordsNotArray_ReturnsMissingRecordsArray()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": \"x\" }");

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.MissingRecordsArray));
        }

        [Test]
        public void Import_RecordElementNotObject_SkipsElementWithRecordNotObject()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ 1, { \"id\": 2, \"name\": \"ok\" } ] }");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual(2, result.Records[0].Id);
            var issue = result.Issues.First(i => i.Kind == ImportIssueKind.RecordNotObject);
            Assert.AreEqual(0, issue.RecordIndex);
        }

        [Test]
        public void Import_IntFieldWithString_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ { \"id\": \"1\", \"name\": \"n\" } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "id");
            Assert.AreEqual(0, result.Records[0].Id);
        }

        [Test]
        public void Import_IntFieldWithDecimal_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"count\": 1.5 } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "count");
            Assert.AreEqual(0, result.Records[0].Count);
        }

        [Test]
        public void Import_BoolFieldWithNumber_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"enabled\": 1 } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "enabled");
        }

        [Test]
        public void Import_StringFieldWithNumber_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"label\": 1 } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "label");
        }

        [Test]
        public void Import_EnumWithInvalidName_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"rarity\": \"Legendary\" } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "rarity");
        }

        [Test]
        public void Import_EnumWithNumericString_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"rarity\": \"1\" } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "rarity");
        }

        [Test]
        public void Import_NonFlagsEnumWithUndefinedInteger_IsTypeMismatch()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"rarity\": 99 } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "rarity");
        }

        [Test]
        public void Import_StringNull_BecomesEmptyWithoutIssue()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"label\": null } ] }");

            Assert.AreEqual(string.Empty, result.Records[0].Label);
            Assert.IsFalse(result.Issues.Any(i => i.FieldName == "label"));
        }

        [Test]
        public void Import_IntOutOfRange_IsValueOutOfRange()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"count\": 2147483648 } ] }");

            AssertIssue(result, ImportIssueKind.ValueOutOfRange, 0, "count");
        }

        [Test]
        public void Import_LongOutOfRange_IsValueOutOfRange()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"bigCount\": 9223372036854775808 } ] }");

            AssertIssue(result, ImportIssueKind.ValueOutOfRange, 0, "bigCount");
        }

        [Test]
        public void Import_FloatOutOfRange_IsValueOutOfRange()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"ratio\": 1e40 } ] }");

            AssertIssue(result, ImportIssueKind.ValueOutOfRange, 0, "ratio");
        }

        [Test]
        public void Import_ListElementTypeMismatch_ReportsIndexedFieldName()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"tags\": [ \"a\", 2 ] } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "tags[1]");
        }

        [Test]
        public void Import_ArrayElementTypeMismatch_ReportsIndexedFieldName()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"values\": [ 1, \"x\", 3 ] } ] }");

            AssertIssue(result, ImportIssueKind.TypeMismatch, 0, "values[1]");
        }

        [Test]
        public void Import_UnknownField_IsWarningAndOtherFieldsImported()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ { \"id\": 1, \"name\": \"n\", \"extra\": 1 } ] }");

            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.UnknownField && i.FieldName == "extra"));
            Assert.AreEqual("n", result.Records[0].Name);
        }

        [Test]
        public void Import_MissingField_IsWarningIncludingId()
        {
            var result = MasterJsonImporter.Import<TestItemRecord>("{ \"records\": [ { \"name\": \"n\" } ] }");

            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.MissingField && i.FieldName == "id"));
        }

        [Test]
        public void Import_UnsupportedNestedField_WarnsOncePerImport()
        {
            const string json = "{ \"records\": [ { \"id\": 1, \"nested\": {} }, { \"id\": 2, \"nested\": {} } ] }";
            var result = MasterJsonImporter.Import<TestAllTypesRecord>(json);

            Assert.AreEqual(1, result.Issues.Count(i => i.Kind == ImportIssueKind.UnsupportedFieldType && i.FieldName == "nested"));
        }

        [Test]
        public void Import_MissingFields_NormalizesStringListAndArray()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1 } ] }");

            var record = result.Records[0];
            Assert.AreEqual(string.Empty, record.Label);
            Assert.IsNotNull(record.Tags);
            Assert.AreEqual(0, record.Tags.Count);
            Assert.IsNotNull(record.Values);
            Assert.AreEqual(0, record.Values.Length);
        }

        [Test]
        public void Import_DateLikeString_StaysStringWithoutDateTimeConversion()
        {
            var result = MasterJsonImporter.Import<TestAllTypesRecord>("{ \"records\": [ { \"id\": 1, \"label\": \"2024-01-01\" } ] }");

            Assert.AreEqual("2024-01-01", result.Records[0].Label);
        }

        [Test]
        public void Import_StringId_IsCaseSensitiveForKeys()
        {
            var result = MasterJsonImporter.Import<TestQuestRecord>(
                "{ \"records\": [ { \"id\": \"Quest_A\", \"title\": \"A\" }, { \"id\": \"quest_a\", \"title\": \"B\" } ] }");

            Assert.AreEqual("A", result.Records[0].Title);
            Assert.AreEqual("B", result.Records[1].Title);
        }

        [Test]
        public void ImportFromFile_Utf8WithAndWithoutBom_ReadsSuccessfully()
        {
            var pathNoBom = Path.Combine(Path.GetTempPath(), "mdt_import_" + Guid.NewGuid().ToString("N") + ".json");
            var pathBom = Path.Combine(Path.GetTempPath(), "mdt_import_" + Guid.NewGuid().ToString("N") + ".json");
            const string json = "{ \"records\": [ { \"id\": 1, \"name\": \"file\" } ] }";

            try
            {
                File.WriteAllText(pathNoBom, json, new UTF8Encoding(false));
                File.WriteAllText(pathBom, json, new UTF8Encoding(true));

                var noBom = MasterJsonImporter.ImportFromFile<TestItemRecord>(pathNoBom);
                var bom = MasterJsonImporter.ImportFromFile<TestItemRecord>(pathBom);

                Assert.IsTrue(noBom.Succeeded);
                Assert.IsTrue(bom.Succeeded);
                Assert.AreEqual("file", noBom.Records[0].Name);
                Assert.AreEqual("file", bom.Records[0].Name);
            }
            finally
            {
                if (File.Exists(pathNoBom))
                {
                    File.Delete(pathNoBom);
                }

                if (File.Exists(pathBom))
                {
                    File.Delete(pathBom);
                }
            }
        }

        [Test]
        public void ImportFromFile_MissingPath_ReturnsFileNotFoundWithoutThrowing()
        {
            var path = Path.Combine(Path.GetTempPath(), "mdt_missing_" + Guid.NewGuid().ToString("N") + ".json");
            var result = MasterJsonImporter.ImportFromFile<TestItemRecord>(path);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.FileNotFound));
        }

        [Test]
        public void Import_AbstractRecordType_ReturnsUnsupportedRecordType()
        {
            var result = MasterJsonImporter.Import<IntIdMasterRecord>("{ \"records\": [] }");

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.UnsupportedRecordType));
        }

        [Test]
        public void Import_IssueSeverity_MatchesSpecification()
        {
            Assert.AreEqual(ImportSeverity.Error, SeverityOf(ImportIssueKind.InvalidJson));
            Assert.AreEqual(ImportSeverity.Error, SeverityOf(ImportIssueKind.DuplicateFieldName));
            Assert.AreEqual(ImportSeverity.Warning, SeverityOf(ImportIssueKind.UnknownField));
        }

        [Test]
        public void RuntimeAssembly_DoesNotReferenceNewtonsoft()
        {
            var referenced = typeof(MasterDataBase).Assembly.GetReferencedAssemblies();
            Assert.IsFalse(referenced.Any(a => a.Name.StartsWith("Newtonsoft", StringComparison.Ordinal)));
        }

        static ImportSeverity SeverityOf(ImportIssueKind kind)
        {
            switch (kind)
            {
                case ImportIssueKind.InvalidJson:
                case ImportIssueKind.FileNotFound:
                case ImportIssueKind.MissingRecordsArray:
                case ImportIssueKind.RecordNotObject:
                case ImportIssueKind.UnsupportedRecordType:
                case ImportIssueKind.DuplicateFieldName:
                case ImportIssueKind.TypeMismatch:
                case ImportIssueKind.ValueOutOfRange:
                    return ImportSeverity.Error;
                default:
                    return ImportSeverity.Warning;
            }
        }

        static void AssertIssue(ImportResult<TestAllTypesRecord> result, ImportIssueKind kind, int recordIndex, string fieldName)
        {
            Assert.IsTrue(result.Issues.Any(i =>
                i.Kind == kind &&
                i.RecordIndex == recordIndex &&
                i.FieldName == fieldName));
        }

        static void AssertIssue(ImportResult<TestItemRecord> result, ImportIssueKind kind, int recordIndex, string fieldName)
        {
            Assert.IsTrue(result.Issues.Any(i =>
                i.Kind == kind &&
                i.RecordIndex == recordIndex &&
                i.FieldName == fieldName));
        }
    }
}
