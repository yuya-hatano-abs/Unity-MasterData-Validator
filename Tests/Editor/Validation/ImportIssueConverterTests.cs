using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ImportIssueConverter"/> のテストです。
    /// </summary>
    public class ImportIssueConverterTests
    {
        static readonly Dictionary<ImportIssueKind, ValidationIssueKind> ExpectedKindMap =
            new Dictionary<ImportIssueKind, ValidationIssueKind>
            {
                { ImportIssueKind.InvalidJson, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.FileNotFound, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.MissingRecordsArray, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.RecordNotObject, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.UnsupportedRecordType, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.DuplicateFieldName, ValidationIssueKind.ImportFailed },
                { ImportIssueKind.TypeMismatch, ValidationIssueKind.TypeMismatch },
                { ImportIssueKind.ValueOutOfRange, ValidationIssueKind.ValueOutOfRange },
                { ImportIssueKind.UnknownField, ValidationIssueKind.UnknownField },
                { ImportIssueKind.MissingField, ValidationIssueKind.MissingField },
                { ImportIssueKind.UnsupportedFieldType, ValidationIssueKind.UnsupportedFieldType },
            };

        [Test]
        public void Convert_AllImportIssueKinds_MatchExpectedMap()
        {
            foreach (ImportIssueKind kind in Enum.GetValues(typeof(ImportIssueKind)))
            {
                var importIssue = new ImportIssue(
                    ImportSeverity.Error,
                    kind,
                    "msg-" + kind,
                    recordIndex: 1,
                    fieldName: "f",
                    lineNumber: 2);

                var converted = ImportIssueConverter.Convert(importIssue, "ItemMaster", "Assets/X.asset");

                Assert.IsTrue(
                    ExpectedKindMap.TryGetValue(kind, out var expectedKind),
                    "ExpectedKindMap に ImportIssueKind." + kind + " がありません。ImportFailed を期待する場合はマップに追加してください。");
                Assert.AreEqual(expectedKind, converted.Kind);
            }
        }

        [Test]
        public void Convert_UnknownEnumValue_FallsBackToImportFailed()
        {
            var importIssue = new ImportIssue(
                ImportSeverity.Error,
                (ImportIssueKind)9999,
                "unknown",
                -1,
                null,
                0);
            var converted = ImportIssueConverter.Convert(importIssue, null, null);
            Assert.AreEqual(ValidationIssueKind.ImportFailed, converted.Kind);
        }

        [Test]
        public void Convert_MapsSeverityAndFields()
        {
            var warning = new ImportIssue(
                ImportSeverity.Warning,
                ImportIssueKind.UnknownField,
                "未知",
                3,
                "extra",
                12);
            var converted = ImportIssueConverter.Convert(warning, "ItemMaster", "Assets/P.asset");

            Assert.AreEqual(ValidationSeverity.Warning, converted.Severity);
            Assert.AreEqual(ValidationIssueKind.UnknownField, converted.Kind);
            Assert.AreEqual(ImportIssueConverter.ValidatorId, converted.ValidatorId);
            Assert.AreEqual("未知", converted.Message);
            Assert.AreEqual("ItemMaster", converted.MasterName);
            Assert.AreEqual("Assets/P.asset", converted.AssetPath);
            Assert.AreEqual(3, converted.RecordIndex);
            Assert.IsNull(converted.RecordKey);
            Assert.AreEqual("extra", converted.FieldName);
            Assert.AreEqual(12, converted.LineNumber);
        }

        [Test]
        public void Convert_AllowsNullMasterNameAndAssetPath()
        {
            var issue = new ImportIssue(ImportSeverity.Error, ImportIssueKind.InvalidJson, "e", -1, null, 0);
            var converted = ImportIssueConverter.Convert(issue, null, null);
            Assert.IsNull(converted.MasterName);
            Assert.IsNull(converted.AssetPath);
        }

        [Test]
        public void ConvertAll_PreservesOrder()
        {
            var issues = new[]
            {
                new ImportIssue(ImportSeverity.Error, ImportIssueKind.InvalidJson, "1", -1, null, 0),
                new ImportIssue(ImportSeverity.Warning, ImportIssueKind.UnknownField, "2", 0, "x", 0),
            };
            var converted = ImportIssueConverter.ConvertAll(issues, "M", null);
            Assert.AreEqual(2, converted.Count);
            Assert.AreEqual("1", converted[0].Message);
            Assert.AreEqual("2", converted[1].Message);
        }

        [Test]
        public void ConvertAll_Empty_ReturnsEmpty()
        {
            var converted = ImportIssueConverter.ConvertAll(Array.Empty<ImportIssue>(), "M", null);
            Assert.AreEqual(0, converted.Count);
        }

        [Test]
        public void ConvertAll_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ImportIssueConverter.ConvertAll(null, "M", null));
        }

        [Test]
        public void Convert_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ImportIssueConverter.Convert(null, "M", null));
        }

        [Test]
        public void Integration_ImportErrorsMatchValidationCounts()
        {
            const string json = "{ \"records\": [ { \"id\": 1, \"name\": 1 } ] }";
            var import = MasterJsonImporter.Import<TestItemRecord>(json);
            var converted = ImportIssueConverter.ConvertAll(import.Issues, "TestItemMaster", null);
            var validation = new ValidationResult(converted);

            Assert.AreEqual(import.Issues.Count(i => i.Severity == ImportSeverity.Error), validation.ErrorCount);
            Assert.AreEqual(import.Issues.Count(i => i.Severity == ImportSeverity.Warning), validation.WarningCount);
        }
    }
}
