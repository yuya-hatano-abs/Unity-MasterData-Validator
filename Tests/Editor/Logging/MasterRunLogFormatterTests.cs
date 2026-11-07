using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Logging;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterRunLogFormatter"/> のテストです。アセットは作りません。
    /// </summary>
    public sealed class MasterRunLogFormatterTests
    {
        [Test]
        public void GetEndLevel_FailureStatuses_AreError()
        {
            Assert.AreEqual(MasterLogLevel.Error, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.ValidationFailed)));
            Assert.AreEqual(MasterLogLevel.Error, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.ImportFailed)));
            Assert.AreEqual(MasterLogLevel.Error, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.SaveFailed)));
            Assert.AreEqual(MasterLogLevel.Error, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.InvalidRequest)));
            Assert.AreEqual(MasterLogLevel.Error, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.UnexpectedError)));
        }

        [Test]
        public void GetEndLevel_SaveCancelled_IsWarning()
        {
            Assert.AreEqual(MasterLogLevel.Warning, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.SaveCancelled)));
        }

        [Test]
        public void GetEndLevel_ValidatedOrSavedWithWarnings_AreWarning()
        {
            var warnings = Issues(ValidationSeverity.Warning, 1);
            Assert.AreEqual(
                MasterLogLevel.Warning,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Validated, warnings)));
            Assert.AreEqual(
                MasterLogLevel.Warning,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, warnings, save: Save(SaveOutcome.Created))));
            Assert.AreEqual(
                MasterLogLevel.Warning,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, warnings, save: Save(SaveOutcome.Updated))));
            Assert.AreEqual(
                MasterLogLevel.Warning,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, warnings, save: Save(SaveOutcome.Unchanged))));
        }

        [Test]
        public void GetEndLevel_ValidatedOrSavedWithoutWarnings_AreInfo()
        {
            Assert.AreEqual(MasterLogLevel.Info, MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Validated)));
            Assert.AreEqual(
                MasterLogLevel.Info,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, save: Save(SaveOutcome.Created))));
            Assert.AreEqual(
                MasterLogLevel.Info,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, save: Save(SaveOutcome.Updated))));
            Assert.AreEqual(
                MasterLogLevel.Info,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.Saved, save: Save(SaveOutcome.Unchanged))));
        }

        [Test]
        public void GetEndLevel_ErrorBeatsWarnings()
        {
            var mixed = new ValidationResult(new[]
            {
                Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId),
                Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField),
            });

            Assert.AreEqual(
                MasterLogLevel.Error,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.ValidationFailed, mixed)));
            Assert.AreEqual(
                MasterLogLevel.Error,
                MasterRunLogFormatter.GetEndLevel(Result(PipelineStatus.SaveFailed, mixed, save: Save(SaveOutcome.Failed))));
        }

        [Test]
        public void GetEndLevel_NullResult_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => MasterRunLogFormatter.GetEndLevel(null));
            Assert.AreEqual("result", exception.ParamName);
        }

        [Test]
        public void FormatStart_ValidateAndSave_IncludesPresentFieldsInOrder()
        {
            var request = new PipelineRequest(
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                @"C:\data\items.json",
                "Assets/Masters/ItemMaster.asset",
                PipelineMode.ValidateAndSave);

            var line = MasterRunLogFormatter.FormatStart(request);

            Assert.AreEqual(
                "[開始] モード: 検証して保存 ・ マスタ: TestItemMaster ・ JSON: C:\\data\\items.json ・ 保存先: Assets/Masters/ItemMaster.asset",
                line);
        }

        [Test]
        public void FormatStart_Validate_OmitsAssetPath()
        {
            var request = new PipelineRequest(
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                @"C:\data\items.json",
                "Assets/Masters/ItemMaster.asset",
                PipelineMode.Validate);

            var line = MasterRunLogFormatter.FormatStart(request);

            Assert.AreEqual(
                "[開始] モード: 検証 ・ マスタ: TestItemMaster ・ JSON: C:\\data\\items.json",
                line);
            StringAssert.DoesNotContain("保存先", line);
        }

        [Test]
        public void FormatStart_OmitsMissingFields()
        {
            var request = new PipelineRequest(
                null,
                typeof(TestItemRecord),
                string.Empty,
                string.Empty,
                PipelineMode.ValidateAndSave);

            var line = MasterRunLogFormatter.FormatStart(request);

            Assert.AreEqual("[開始] モード: 検証して保存", line);
        }

        [Test]
        public void FormatStart_NullRequest_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => MasterRunLogFormatter.FormatStart(null));
            Assert.AreEqual("request", exception.ParamName);
        }

        [Test]
        public void FormatEnd_SavedCreated_IncludesCountsAndDuration()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.Saved, table: Table(10), save: Save(SaveOutcome.Created)),
                123);

            Assert.AreEqual(
                "[終了] 保存しました(作成) ・ レコード: 10 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 123 ms",
                line);
            StringAssert.DoesNotContain("保存結果:", line);
        }

        [Test]
        public void FormatEnd_SavedUpdatedUnchangedAndPlain()
        {
            Assert.AreEqual(
                "[終了] 保存しました(更新) ・ レコード: 1 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 1 ms",
                MasterRunLogFormatter.FormatEnd(
                    Result(PipelineStatus.Saved, table: Table(1), save: Save(SaveOutcome.Updated)),
                    1));
            Assert.AreEqual(
                "[終了] 内容が同じため、書き込みませんでした ・ レコード: 1 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 1 ms",
                MasterRunLogFormatter.FormatEnd(
                    Result(PipelineStatus.Saved, table: Table(1), save: Save(SaveOutcome.Unchanged)),
                    1));
            Assert.AreEqual(
                "[終了] 保存しました ・ レコード: 1 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 1 ms",
                MasterRunLogFormatter.FormatEnd(
                    Result(PipelineStatus.Saved, table: Table(1)),
                    1));
        }

        [Test]
        public void FormatEnd_SaveCancelled_UsesCancelPhrase()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.SaveCancelled, table: Table(2)),
                4);

            Assert.AreEqual(
                "[終了] 保存をキャンセルしました ・ レコード: 2 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 4 ms",
                line);
        }

        [Test]
        public void FormatEnd_SaveFailed_UsesFailurePhrase()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.SaveFailed, table: Table(2), save: Save(SaveOutcome.Failed)),
                5);

            Assert.AreEqual(
                "[終了] 保存に失敗しました ・ レコード: 2 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 5 ms",
                line);
        }

        [Test]
        public void FormatEnd_Validated_OmitsSavePhrase()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.Validated, table: Table(2)),
                6);

            Assert.AreEqual(
                "[終了] 検証しました ・ レコード: 2 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 6 ms",
                line);
        }

        [Test]
        public void FormatEnd_ValidationFailed_UsesFailurePhrase()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.ValidationFailed, Issues(ValidationSeverity.Error, 1), Table(2)),
                7);

            Assert.AreEqual(
                "[終了] 検証に失敗しました ・ レコード: 2 件 ・ エラー: 1 ・ 警告: 0 ・ 所要時間: 7 ms",
                line);
        }

        [Test]
        public void FormatEnd_ImportFailed_IncludesRecordCount()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.ImportFailed, table: Table(0), message: "JSON を読めません。"),
                8);

            Assert.AreEqual(
                "[終了] インポートに失敗しました ・ 理由: JSON を読めません。 ・ レコード: 0 件 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 8 ms",
                line);
        }

        [Test]
        public void FormatEnd_InvalidRequest_IncludesReasonWhenMessagePresent()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.InvalidRequest, message: "マスタ型が指定されていません。"),
                9);

            Assert.AreEqual(
                "[終了] 依頼が不正です ・ 理由: マスタ型が指定されていません。 ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 9 ms",
                line);
            StringAssert.DoesNotContain("レコード:", line);
        }

        [Test]
        public void FormatEnd_EmptyMessage_OmitsReason()
        {
            var line = MasterRunLogFormatter.FormatEnd(Result(PipelineStatus.Validated, table: Table(1)), 1);
            StringAssert.DoesNotContain("理由:", line);
        }

        [Test]
        public void FormatEnd_UnexpectedError_IncludesReason()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.UnexpectedError, message: "InvalidOperationException: boom"),
                3);

            Assert.AreEqual(
                "[終了] 実行できませんでした ・ 理由: InvalidOperationException: boom ・ エラー: 0 ・ 警告: 0 ・ 所要時間: 3 ms",
                line);
        }

        [Test]
        public void FormatEnd_NegativeElapsed_PrintsZero()
        {
            var line = MasterRunLogFormatter.FormatEnd(Result(PipelineStatus.Validated, table: Table(1)), -5);
            StringAssert.Contains("所要時間: 0 ms", line);
        }

        [Test]
        public void FormatEnd_LargeElapsed_UsesInvariantDigits()
        {
            var line = MasterRunLogFormatter.FormatEnd(Result(PipelineStatus.Validated, table: Table(1)), 1000);
            StringAssert.Contains("所要時間: 1000 ms", line);
        }

        [Test]
        public void FormatEnd_MessageNewlines_CollapsedToOneLine()
        {
            var line = MasterRunLogFormatter.FormatEnd(
                Result(PipelineStatus.InvalidRequest, message: "1 行目\r\n2 行目\n3 行目\r4 行目"),
                1);

            StringAssert.Contains("理由: 1 行目 2 行目 3 行目 4 行目", line);
            StringAssert.DoesNotContain("\r", line);
            StringAssert.DoesNotContain("\n", line);
        }

        [Test]
        public void FormatEnd_NullResult_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => MasterRunLogFormatter.FormatEnd(null, 0));
            Assert.AreEqual("result", exception.ParamName);
        }

        static PipelineResult Result(
            PipelineStatus status,
            ValidationResult validation = null,
            MasterTable table = null,
            PipelineSaveInfo save = null,
            string message = "")
        {
            return new PipelineResult(status, validation, table, false, save, message);
        }

        static ValidationResult Issues(ValidationSeverity severity, int count)
        {
            var issues = new List<ValidationIssue>();
            for (var i = 0; i < count; i++)
            {
                var kind = severity == ValidationSeverity.Error
                    ? ValidationIssueKind.DuplicateId
                    : ValidationIssueKind.UnknownField;
                issues.Add(Issue(severity, kind));
            }

            return new ValidationResult(issues);
        }

        static ValidationIssue Issue(ValidationSeverity severity, ValidationIssueKind kind)
        {
            return new ValidationIssue(severity, kind, "test", "問題です。");
        }

        static MasterTable Table(int count)
        {
            var records = new List<IMasterRecord>();
            for (var i = 0; i < count; i++)
            {
                records.Add(new TestItemRecord(i + 1, "Item"));
            }

            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                records,
                null);
        }

        static PipelineSaveInfo Save(SaveOutcome outcome)
        {
            return new PipelineSaveInfo(outcome, "Assets/Item.asset", 0, 1, null);
        }
    }
}
