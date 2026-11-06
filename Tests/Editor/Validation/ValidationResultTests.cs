using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationIssue"/> と <see cref="ValidationResult"/> のテストです。
    /// </summary>
    public class ValidationResultTests
    {
        [Test]
        public void ValidationIssue_Constructor_ExposesAllProperties()
        {
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.TypeMismatch,
                "test-validator",
                "メッセージ",
                masterName: "ItemMaster",
                assetPath: "Assets/X.asset",
                recordIndex: 2,
                recordKey: "42",
                fieldName: "name",
                lineNumber: 10);

            Assert.AreEqual(ValidationSeverity.Error, issue.Severity);
            Assert.AreEqual(ValidationIssueKind.TypeMismatch, issue.Kind);
            Assert.AreEqual("test-validator", issue.ValidatorId);
            Assert.AreEqual("メッセージ", issue.Message);
            Assert.AreEqual("ItemMaster", issue.MasterName);
            Assert.AreEqual("Assets/X.asset", issue.AssetPath);
            Assert.AreEqual(2, issue.RecordIndex);
            Assert.AreEqual("42", issue.RecordKey);
            Assert.AreEqual("name", issue.FieldName);
            Assert.AreEqual(10, issue.LineNumber);
        }

        [Test]
        public void ValidationIssue_OptionalDefaults_AreNullOrSentinel()
        {
            var issue = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.UnknownField,
                "import",
                "msg");

            Assert.IsNull(issue.MasterName);
            Assert.IsNull(issue.AssetPath);
            Assert.AreEqual(-1, issue.RecordIndex);
            Assert.IsNull(issue.RecordKey);
            Assert.IsNull(issue.FieldName);
            Assert.AreEqual(0, issue.LineNumber);
        }

        [Test]
        public void ValidationIssue_NullValidatorId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, null, "msg"));
        }

        [Test]
        public void ValidationIssue_NullMessage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "id", null));
        }

        [Test]
        public void Empty_HasZeroIssuesAndIsValid()
        {
            var result = ValidationResult.Empty;

            Assert.AreEqual(0, result.Issues.Count);
            Assert.AreEqual(0, result.ErrorCount);
            Assert.AreEqual(0, result.WarningCount);
            Assert.IsFalse(result.HasErrors);
            Assert.IsFalse(result.HasWarnings);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Result_CountsErrorsAndWarnings()
        {
            var issues = new[]
            {
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "a", "e1"),
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.TypeMismatch, "a", "e2"),
                new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "a", "w1"),
            };
            var result = new ValidationResult(issues);

            Assert.AreEqual(2, result.ErrorCount);
            Assert.AreEqual(1, result.WarningCount);
            Assert.IsTrue(result.HasErrors);
            Assert.IsTrue(result.HasWarnings);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void Result_WarningsOnly_IsValid()
        {
            var result = new ValidationResult(new[]
            {
                new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "import", "w"),
            });

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void Result_IsImmutableAgainstInputListMutation()
        {
            var list = new List<ValidationIssue>
            {
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "a", "e"),
            };
            var result = new ValidationResult(list);
            list.Clear();

            Assert.AreEqual(1, result.Issues.Count);
        }

        [Test]
        public void Result_NullIssues_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationResult(null));
        }

        [Test]
        public void Result_NullElement_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ValidationResult(new ValidationIssue[] { null }));
        }

        [Test]
        public void Combine_PreservesOrderAndCounts()
        {
            var first = new ValidationResult(new[]
            {
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "a", "e1"),
            });
            var second = new ValidationResult(new[]
            {
                new ValidationIssue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "b", "w1"),
                new ValidationIssue(ValidationSeverity.Error, ValidationIssueKind.TypeMismatch, "b", "e2"),
            });

            var combined = ValidationResult.Combine(new[] { first, second, ValidationResult.Empty });

            Assert.AreEqual(3, combined.Issues.Count);
            Assert.AreEqual(2, combined.ErrorCount);
            Assert.AreEqual(1, combined.WarningCount);
            Assert.AreEqual(ValidationIssueKind.ImportFailed, combined.Issues[0].Kind);
            Assert.AreEqual(ValidationIssueKind.UnknownField, combined.Issues[1].Kind);
            Assert.AreEqual(ValidationIssueKind.TypeMismatch, combined.Issues[2].Kind);
        }

        [Test]
        public void Combine_EmptyCollection_ReturnsEmpty()
        {
            var combined = ValidationResult.Combine(Array.Empty<ValidationResult>());
            Assert.AreEqual(0, combined.Issues.Count);
            Assert.IsTrue(combined.IsValid);
        }

        [Test]
        public void Combine_NullCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ValidationResult.Combine(null));
        }

        [Test]
        public void Combine_NullElement_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                ValidationResult.Combine(new[] { ValidationResult.Empty, null }));
        }
    }
}
