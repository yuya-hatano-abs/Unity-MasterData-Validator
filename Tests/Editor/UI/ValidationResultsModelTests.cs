using System.Collections.Generic;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationResultsModel"/> のテストです。
    /// </summary>
    public sealed class ValidationResultsModelTests
    {
        [Test]
        public void InitialState_ShowsBothSeverities()
        {
            var model = new ValidationResultsModel();

            Assert.AreEqual(0, model.AllIssues.Count);
            Assert.IsTrue(model.ShowErrors);
            Assert.IsTrue(model.ShowWarnings);
            Assert.IsNull(model.KindFilter);
            Assert.AreEqual(0, model.ErrorCount);
            Assert.AreEqual(0, model.WarningCount);
            Assert.AreEqual(0, model.VisibleIssues.Count);
            Assert.AreEqual(0, model.AvailableKinds.Count);
        }

        [Test]
        public void SetIssues_CountsSkipNullsAndNullInput()
        {
            var model = new ValidationResultsModel();
            var error = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e");
            var warning = Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w");

            model.SetIssues(new ValidationIssue[] { null, error, null, warning });

            Assert.AreEqual(2, model.AllIssues.Count);
            Assert.AreEqual(1, model.ErrorCount);
            Assert.AreEqual(1, model.WarningCount);
            Assert.AreSame(error, model.AllIssues[0]);
            Assert.AreSame(warning, model.AllIssues[1]);

            model.SetIssues(null);

            Assert.AreEqual(0, model.AllIssues.Count);
            Assert.AreEqual(0, model.ErrorCount);
            Assert.AreEqual(0, model.WarningCount);
        }

        [Test]
        public void VisibleIssues_PreserveOrderAndCombineFilters()
        {
            var model = new ValidationResultsModel();
            var errorDup = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e1");
            var warningUnknown = Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w1");
            var errorRequired = Issue(ValidationSeverity.Error, ValidationIssueKind.MissingRequiredField, "e2");
            var warningDup = Issue(ValidationSeverity.Warning, ValidationIssueKind.DuplicateId, "w2");
            model.SetIssues(new[] { errorDup, warningUnknown, errorRequired, warningDup });

            model.ShowWarnings = false;
            var errorsOnly = model.VisibleIssues;
            Assert.AreEqual(2, errorsOnly.Count);
            Assert.AreSame(errorDup, errorsOnly[0]);
            Assert.AreSame(errorRequired, errorsOnly[1]);

            model.ShowWarnings = true;
            model.ShowErrors = false;
            model.KindFilter = ValidationIssueKind.UnknownField;
            var unknownOnly = model.VisibleIssues;
            Assert.AreEqual(1, unknownOnly.Count);
            Assert.AreSame(warningUnknown, unknownOnly[0]);

            model.ShowErrors = true;
            model.KindFilter = ValidationIssueKind.DuplicateId;
            var duplicates = model.VisibleIssues;
            Assert.AreEqual(2, duplicates.Count);
            Assert.AreSame(errorDup, duplicates[0]);
            Assert.AreSame(warningDup, duplicates[1]);
        }

        [Test]
        public void Counts_IgnoreFilters()
        {
            var model = new ValidationResultsModel();
            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e1"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w1"),
                Issue(ValidationSeverity.Error, ValidationIssueKind.MissingRequiredField, "e2"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.DuplicateId, "w2"),
            });
            model.ShowErrors = false;
            model.ShowWarnings = false;
            model.KindFilter = ValidationIssueKind.DuplicateId;

            Assert.AreEqual(0, model.VisibleIssues.Count);
            Assert.AreEqual(2, model.ErrorCount);
            Assert.AreEqual(2, model.WarningCount);
        }

        [Test]
        public void AvailableKinds_AreDeclarationOrder()
        {
            var model = new ValidationResultsModel();
            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.AmbiguousMasterName, "a"),
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "d"),
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "d2"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.MissingRequiredField, "m"),
            });

            Assert.AreEqual(3, model.AvailableKinds.Count);
            Assert.AreEqual(ValidationIssueKind.DuplicateId, model.AvailableKinds[0]);
            Assert.AreEqual(ValidationIssueKind.MissingRequiredField, model.AvailableKinds[1]);
            Assert.AreEqual(ValidationIssueKind.AmbiguousMasterName, model.AvailableKinds[2]);
        }

        [Test]
        public void SetIssues_ResetsKindFilterWhenMissing()
        {
            var model = new ValidationResultsModel();
            model.KindFilter = ValidationIssueKind.DuplicateId;

            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w"),
            });

            Assert.IsNull(model.KindFilter);
        }

        [Test]
        public void SetIssues_KeepsKindFilterWhenPresent()
        {
            var model = new ValidationResultsModel();
            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e"),
            });
            model.KindFilter = ValidationIssueKind.DuplicateId;

            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.DuplicateId, "w"),
            });

            Assert.AreEqual(ValidationIssueKind.DuplicateId, model.KindFilter);
        }

        [Test]
        public void SetIssues_DoesNotChangeSeverityToggles()
        {
            var model = new ValidationResultsModel();
            model.ShowErrors = false;
            model.ShowWarnings = false;

            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e"),
            });

            Assert.IsFalse(model.ShowErrors);
            Assert.IsFalse(model.ShowWarnings);
        }

        [Test]
        public void Clear_EmptiesIssuesAndKeepsToggles()
        {
            var model = new ValidationResultsModel();
            model.ShowErrors = false;
            model.SetIssues(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e"),
            });
            model.KindFilter = ValidationIssueKind.DuplicateId;

            model.Clear();

            Assert.AreEqual(0, model.AllIssues.Count);
            Assert.AreEqual(0, model.VisibleIssues.Count);
            Assert.IsFalse(model.ShowErrors);
            Assert.IsTrue(model.ShowWarnings);
            Assert.IsNull(model.KindFilter);
        }

        [Test]
        public void SetIssues_CopiesSoCallerMutationDoesNotAffect()
        {
            var model = new ValidationResultsModel();
            var issue = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "e");
            var source = new List<ValidationIssue> { issue };

            model.SetIssues(source);
            source.Add(Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "w"));
            source.Clear();

            Assert.AreEqual(1, model.AllIssues.Count);
            Assert.AreSame(issue, model.AllIssues[0]);
        }

        static ValidationIssue Issue(ValidationSeverity severity, ValidationIssueKind kind, string message)
        {
            return new ValidationIssue(severity, kind, "test", message);
        }
    }
}
