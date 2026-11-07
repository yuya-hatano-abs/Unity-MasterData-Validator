using System.Collections.Generic;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="PipelineResultFormatter"/> のテストです。
    /// </summary>
    public sealed class PipelineResultFormatterTests
    {
        [Test]
        public void Format_Validated_NoProblems()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Validated,
                ValidationResult.Empty,
                Table(2),
                true,
                null,
                string.Empty));

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Info, lines[0].Severity);
            Assert.AreEqual("検証しました: エラー 0 件、警告 0 件。保存できます。(レコード 2 件)", lines[0].Text);
        }

        [Test]
        public void Format_Validated_WithWarnings()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "追加のフィールドです。"),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.MissingField, "フィールドがありません。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Validated,
                validation,
                Table(1),
                true,
                null,
                string.Empty));

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Warning, lines[0].Severity);
            Assert.AreEqual("検証しました: エラー 0 件、警告 2 件。保存できます。(レコード 1 件)", lines[0].Text);
            Assert.AreEqual(ResultLineSeverity.Warning, lines[1].Severity);
            Assert.AreEqual("[警告] 追加のフィールドです。", lines[1].Text);
            Assert.AreEqual("[警告] フィールドがありません。", lines[2].Text);
        }

        [Test]
        public void Format_ValidationFailed()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "ID が重複しています。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.ValidationFailed,
                validation,
                Table(2),
                false,
                null,
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Error, lines[0].Severity);
            Assert.AreEqual(
                "検証しました: エラー 1 件、警告 0 件。エラーがあるため、保存できません。(レコード 2 件)",
                lines[0].Text);
            Assert.AreEqual(ResultLineSeverity.Error, lines[1].Severity);
            Assert.AreEqual("[エラー] ID が重複しています。", lines[1].Text);
        }

        [Test]
        public void Format_ImportFailed()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "JSON を読めません。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.ImportFailed,
                validation,
                Table(0),
                false,
                null,
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Error, lines[0].Severity);
            Assert.AreEqual("インポートに失敗しました。(エラー 1 件、警告 0 件)", lines[0].Text);
            Assert.IsFalse(lines[0].Text.Contains("レコード"));
            Assert.AreEqual("[エラー] JSON を読めません。", lines[1].Text);
        }

        [Test]
        public void Format_InvalidRequest_OmitsIssues()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "出てはいけない。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.InvalidRequest,
                validation,
                null,
                false,
                null,
                "理由です"));

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Error, lines[0].Severity);
            Assert.AreEqual("実行できませんでした: 理由です", lines[0].Text);
        }

        [Test]
        public void Format_UnexpectedError()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.UnexpectedError,
                ValidationResult.Empty,
                null,
                false,
                null,
                "InvalidOperationException: boom"));

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Error, lines[0].Severity);
            Assert.AreEqual("実行できませんでした: InvalidOperationException: boom", lines[0].Text);
        }

        [Test]
        public void Format_SavedCreated()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(3),
                true,
                new PipelineSaveInfo(SaveOutcome.Created, "Assets/A.asset", -1, 3, null),
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Info, lines[0].Severity);
            Assert.AreEqual("検証しました: エラー 0 件、警告 0 件。(レコード 3 件)", lines[0].Text);
            Assert.AreEqual(ResultLineSeverity.Info, lines[1].Severity);
            Assert.AreEqual("保存しました(新規作成): Assets/A.asset(3 件)", lines[1].Text);
        }

        [Test]
        public void Format_SavedUpdated()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(4),
                true,
                new PipelineSaveInfo(SaveOutcome.Updated, "Assets/A.asset", 1, 4, null),
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("保存しました(更新): Assets/A.asset(1 → 4 件)", lines[1].Text);
        }

        [Test]
        public void Format_SavedUnchanged()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(2),
                true,
                new PipelineSaveInfo(SaveOutcome.Unchanged, "Assets/A.asset", 2, 2, null),
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("内容が同じため、書き込みませんでした: Assets/A.asset", lines[1].Text);
        }

        [Test]
        public void Format_SaveCancelled()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.SaveCancelled,
                ValidationResult.Empty,
                Table(2),
                true,
                null,
                string.Empty));

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual(ResultLineSeverity.Info, lines[0].Severity);
            Assert.AreEqual("検証しました: エラー 0 件、警告 0 件。(レコード 2 件)", lines[0].Text);
            Assert.AreEqual(ResultLineSeverity.Warning, lines[1].Severity);
            Assert.AreEqual("保存をキャンセルしました。", lines[1].Text);
        }

        [Test]
        public void Format_SaveFailed()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.SaveFailed,
                ValidationResult.Empty,
                Table(1),
                true,
                new PipelineSaveInfo(
                    SaveOutcome.Failed,
                    "Assets/A.asset",
                    -1,
                    -1,
                    new[]
                    {
                        new SaveIssue(SaveIssueKind.AssetTypeMismatch, "型が違います。"),
                        new SaveIssue(SaveIssueKind.AssetExists, "既にあります。"),
                    }),
                string.Empty));

            Assert.AreEqual(4, lines.Count);
            Assert.AreEqual("検証しました: エラー 0 件、警告 0 件。(レコード 1 件)", lines[0].Text);
            Assert.AreEqual(ResultLineSeverity.Error, lines[1].Severity);
            Assert.AreEqual("保存に失敗しました。", lines[1].Text);
            Assert.AreEqual(ResultLineSeverity.Error, lines[2].Severity);
            Assert.AreEqual("型が違います。", lines[2].Text);
            Assert.AreEqual("既にあります。", lines[3].Text);
        }

        [Test]
        public void Format_MaxIssueLines_Truncates()
        {
            var issues = new List<ValidationIssue>();
            for (var i = 0; i < 5; i++)
            {
                issues.Add(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "問題" + i));
            }

            var lines = PipelineResultFormatter.Format(
                Result(
                    PipelineStatus.ValidationFailed,
                    new ValidationResult(issues),
                    Table(5),
                    false,
                    null,
                    string.Empty),
                3);

            Assert.AreEqual(5, lines.Count);
            Assert.AreEqual("[エラー] 問題0", lines[1].Text);
            Assert.AreEqual("[エラー] 問題1", lines[2].Text);
            Assert.AreEqual("[エラー] 問題2", lines[3].Text);
            Assert.AreEqual(ResultLineSeverity.Info, lines[4].Severity);
            Assert.AreEqual("…ほか 2 件", lines[4].Text);
        }

        [Test]
        public void Format_MaxIssueLines_ExactFit_HasNoRemainder()
        {
            var issues = new List<ValidationIssue>();
            for (var i = 0; i < 3; i++)
            {
                issues.Add(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "問題" + i));
            }

            var lines = PipelineResultFormatter.Format(
                Result(
                    PipelineStatus.ValidationFailed,
                    new ValidationResult(issues),
                    Table(3),
                    false,
                    null,
                    string.Empty),
                3);

            Assert.AreEqual(4, lines.Count);
            for (var i = 0; i < lines.Count; i++)
            {
                Assert.IsFalse(lines[i].Text.Contains("ほか"));
            }
        }

        [Test]
        public void Format_LineOrder_IsSummaryThenSaveThenIssues()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "追加です。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Saved,
                validation,
                Table(1),
                true,
                new PipelineSaveInfo(SaveOutcome.Created, "Assets/A.asset", -1, 1, null),
                string.Empty));

            Assert.AreEqual(3, lines.Count);
            StringAssert.StartsWith("検証しました:", lines[0].Text);
            Assert.AreEqual("保存しました(新規作成): Assets/A.asset(1 件)", lines[1].Text);
            Assert.AreEqual("[警告] 追加です。", lines[2].Text);
        }

        [Test]
        public void Format_Validated_OmitsSaveLines()
        {
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.Validated,
                ValidationResult.Empty,
                Table(1),
                true,
                new PipelineSaveInfo(SaveOutcome.Created, "Assets/A.asset", -1, 1, null),
                string.Empty));

            Assert.AreEqual(1, lines.Count);
            StringAssert.Contains("保存できます", lines[0].Text);
            Assert.IsFalse(lines[0].Text.Contains("保存しました"));
        }

        [Test]
        public void Format_IssueSeverity_MatchesValidationIssue()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "警告です。"),
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "エラーです。"),
            });
            var lines = PipelineResultFormatter.Format(Result(
                PipelineStatus.ValidationFailed,
                validation,
                Table(1),
                false,
                null,
                string.Empty));

            Assert.AreEqual(ResultLineSeverity.Warning, lines[1].Severity);
            Assert.AreEqual("[警告] 警告です。", lines[1].Text);
            Assert.AreEqual(ResultLineSeverity.Error, lines[2].Severity);
            Assert.AreEqual("[エラー] エラーです。", lines[2].Text);
        }

        [Test]
        public void FormatHeader_Null_IsSingleError()
        {
            var header = PipelineResultFormatter.FormatHeader(null);
            var full = PipelineResultFormatter.Format(null);

            Assert.AreEqual(1, header.Count);
            Assert.AreEqual(ResultLineSeverity.Error, header[0].Severity);
            Assert.AreEqual("実行できませんでした: 結果がありません。", header[0].Text);
            AssertSameLine(full[0], header[0]);
        }

        [Test]
        public void FormatHeader_Validated_IsSummaryOnly()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "追加です。"),
            });
            var result = Result(PipelineStatus.Validated, validation, Table(1), true, null, string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 1 件。保存できます。(レコード 1 件)");
            Assert.AreEqual(2, PipelineResultFormatter.Format(result).Count);
        }

        [Test]
        public void FormatHeader_ValidationFailed_OmitsIssues()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "ID が重複しています。"),
            });
            var result = Result(PipelineStatus.ValidationFailed, validation, Table(2), false, null, string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 1 件、警告 0 件。エラーがあるため、保存できません。(レコード 2 件)");
            Assert.AreEqual(2, PipelineResultFormatter.Format(result).Count);
        }

        [Test]
        public void FormatHeader_SavedCreated_IncludesSaveLine()
        {
            var result = Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(3),
                true,
                new PipelineSaveInfo(SaveOutcome.Created, "Assets/A.asset", -1, 3, null),
                string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 0 件。(レコード 3 件)",
                "保存しました(新規作成): Assets/A.asset(3 件)");
        }

        [Test]
        public void FormatHeader_SavedUpdated_IncludesSaveLine()
        {
            var result = Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(4),
                true,
                new PipelineSaveInfo(SaveOutcome.Updated, "Assets/A.asset", 1, 4, null),
                string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 0 件。(レコード 4 件)",
                "保存しました(更新): Assets/A.asset(1 → 4 件)");
        }

        [Test]
        public void FormatHeader_SavedUnchanged_IncludesSaveLine()
        {
            var result = Result(
                PipelineStatus.Saved,
                ValidationResult.Empty,
                Table(2),
                true,
                new PipelineSaveInfo(SaveOutcome.Unchanged, "Assets/A.asset", 2, 2, null),
                string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 0 件。(レコード 2 件)",
                "内容が同じため、書き込みませんでした: Assets/A.asset");
        }

        [Test]
        public void FormatHeader_SaveCancelled_IncludesCancelLine()
        {
            var result = Result(
                PipelineStatus.SaveCancelled,
                ValidationResult.Empty,
                Table(2),
                true,
                null,
                string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 0 件。(レコード 2 件)",
                "保存をキャンセルしました。");
        }

        [Test]
        public void FormatHeader_SaveFailed_IncludesFailureLines()
        {
            var result = Result(
                PipelineStatus.SaveFailed,
                ValidationResult.Empty,
                Table(1),
                true,
                new PipelineSaveInfo(
                    SaveOutcome.Failed,
                    "Assets/A.asset",
                    -1,
                    -1,
                    new[]
                    {
                        new SaveIssue(SaveIssueKind.AssetTypeMismatch, "型が違います。"),
                        new SaveIssue(SaveIssueKind.AssetExists, "既にあります。"),
                    }),
                string.Empty);

            AssertHeader(
                result,
                "検証しました: エラー 0 件、警告 0 件。(レコード 1 件)",
                "保存に失敗しました。",
                "型が違います。",
                "既にあります。");
        }

        [Test]
        public void FormatHeader_ImportFailed_IsSummaryOnly()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.ImportFailed, "JSON を読めません。"),
            });
            var result = Result(PipelineStatus.ImportFailed, validation, Table(0), false, null, string.Empty);

            AssertHeader(result, "インポートに失敗しました。(エラー 1 件、警告 0 件)");
            Assert.AreEqual(2, PipelineResultFormatter.Format(result).Count);
        }

        [Test]
        public void FormatHeader_InvalidRequest_IsSingleError()
        {
            var validation = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "出てはいけない。"),
            });
            var result = Result(
                PipelineStatus.InvalidRequest,
                validation,
                null,
                false,
                null,
                "理由です");

            AssertHeader(result, "実行できませんでした: 理由です");
            Assert.AreEqual(1, PipelineResultFormatter.Format(result).Count);
        }

        [Test]
        public void FormatHeader_UnexpectedError_IsSingleError()
        {
            var result = Result(
                PipelineStatus.UnexpectedError,
                ValidationResult.Empty,
                null,
                false,
                null,
                "InvalidOperationException: boom");

            AssertHeader(result, "実行できませんでした: InvalidOperationException: boom");
        }

        [Test]
        public void Format_EqualsHeaderFollowedByIssueLines()
        {
            var issues = new List<ValidationIssue>();
            for (var i = 0; i < 3; i++)
            {
                issues.Add(Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "問題" + i));
            }

            var result = Result(
                PipelineStatus.ValidationFailed,
                new ValidationResult(issues),
                Table(3),
                false,
                null,
                string.Empty);
            var header = PipelineResultFormatter.FormatHeader(result);
            var full = PipelineResultFormatter.Format(result, 2);

            Assert.AreEqual(1, header.Count);
            Assert.AreEqual(4, full.Count);
            AssertSameLine(full[0], header[0]);
            Assert.AreEqual("[エラー] 問題0", full[1].Text);
            Assert.AreEqual("[エラー] 問題1", full[2].Text);
            Assert.AreEqual("…ほか 1 件", full[3].Text);
        }

        static void AssertHeader(PipelineResult result, params string[] texts)
        {
            var header = PipelineResultFormatter.FormatHeader(result);
            var full = PipelineResultFormatter.Format(result);
            Assert.AreEqual(texts.Length, header.Count);
            for (var i = 0; i < texts.Length; i++)
            {
                Assert.AreEqual(texts[i], header[i].Text);
                AssertSameLine(full[i], header[i]);
            }
        }

        static void AssertSameLine(ResultLine actual, ResultLine expected)
        {
            Assert.AreEqual(expected.Severity, actual.Severity);
            Assert.AreEqual(expected.Text, actual.Text);
        }

        static PipelineResult Result(
            PipelineStatus status,
            ValidationResult validation,
            MasterTable table,
            bool canSave,
            PipelineSaveInfo save,
            string message)
        {
            return new PipelineResult(status, validation, table, canSave, save, message);
        }

        static MasterTable Table(int recordCount)
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                new IMasterRecord[recordCount],
                null);
        }

        static ValidationIssue Issue(ValidationSeverity severity, ValidationIssueKind kind, string message)
        {
            return new ValidationIssue(severity, kind, "test", message, "TestItemMaster");
        }
    }
}
