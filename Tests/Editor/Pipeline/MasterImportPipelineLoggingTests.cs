using System;
using System.Collections.Generic;
using System.IO;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Logging;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterImportPipeline"/> が実行 1 回につき開始と終了を記録することのテストです。
    /// Console には出しません。
    /// </summary>
    public sealed class MasterImportPipelineLoggingTests
    {
        const string ItemJsonOne = "{ \"records\": [ { \"id\": 1, \"name\": \"Potion\" } ] }";
        const string ItemJsonTwo =
            "{ \"records\": [ { \"id\": 1, \"name\": \"Potion\" }, { \"id\": 2, \"name\": \"Ether\" } ] }";

        string folder;
        string jsonPath;
        FakeConfirm confirm;
        RecordingLogger logger;
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
            jsonPath = Path.Combine(Path.GetTempPath(), "mdt-pipeline-log-" + Guid.NewGuid().ToString("N") + ".json");
            confirm = new FakeConfirm();
            logger = new RecordingLogger();
            pipeline = new MasterImportPipeline(confirm, null, logger);
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
        public void Execute_WithoutLogger_ValidatedWithoutThrowing()
        {
            Write(ItemJsonOne);
            var silent = new MasterImportPipeline(confirm);
            var result = silent.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            Assert.AreEqual(0, result.Validation.ErrorCount);
            Assert.AreEqual(0, result.Validation.WarningCount);
            Assert.IsNull(result.Save);
            Assert.AreEqual(0, logger.Messages.Count);
        }

        [Test]
        public void Execute_Validate_NoProblems_LogsStartInfoAndEndInfo()
        {
            Write(ItemJsonOne);
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            AssertPair(MasterLogLevel.Info);
            StringAssert.Contains("モード: 検証", logger.Messages[0]);
            StringAssert.Contains(jsonPath, logger.Messages[0]);
            StringAssert.DoesNotContain("保存先", logger.Messages[0]);
            StringAssert.Contains("検証しました", logger.Messages[1]);
            StringAssert.Contains("レコード: 1 件", logger.Messages[1]);
            StringAssert.Contains("エラー: 0", logger.Messages[1]);
            StringAssert.Contains("警告: 0", logger.Messages[1]);
            StringAssert.IsMatch(@"所要時間: \d+ ms", logger.Messages[1]);
        }

        [Test]
        public void Execute_WarningsOnly_EndIsWarning()
        {
            Write("{ \"records\": [ { \"id\": 1, \"name\": \"Potion\", \"extra\": 1 } ] }");
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.GreaterOrEqual(result.Validation.WarningCount, 1);
            AssertPair(MasterLogLevel.Warning);
            StringAssert.Contains("警告: " + result.Validation.WarningCount, logger.Messages[1]);
        }

        [Test]
        public void Execute_ValidationFailed_EndIsErrorAndOnlyTwoLines()
        {
            Write("{ \"records\": [ { \"id\": 1, \"name\": \"A\" }, { \"id\": 1, \"name\": \"B\" } ] }");
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ValidationFailed, result.Status);
            Assert.GreaterOrEqual(result.Validation.ErrorCount, 1);
            AssertPair(MasterLogLevel.Error);
            StringAssert.Contains("検証に失敗しました", logger.Messages[1]);
            StringAssert.Contains("エラー: " + result.Validation.ErrorCount, logger.Messages[1]);
        }

        [Test]
        public void Execute_ImportFailed_EndIsError()
        {
            Write("{");
            var result = pipeline.Execute(ItemRequest(PipelineMode.Validate), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.ImportFailed, result.Status);
            Assert.IsNotNull(result.Table);
            AssertPair(MasterLogLevel.Error);
            StringAssert.Contains("インポートに失敗しました", logger.Messages[1]);
            StringAssert.Contains("レコード: " + result.Table.Records.Count + " 件", logger.Messages[1]);
        }

        [Test]
        public void Execute_SaveCancelled_EndIsWarning()
        {
            Write(ItemJsonOne);
            var created = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);
            Assert.AreEqual(SaveOutcome.Created, created.Save.Outcome);
            AssertPair(MasterLogLevel.Info);

            logger.Clear();
            Write(ItemJsonTwo);
            confirm.Result = false;
            var cancelled = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.SaveCancelled, cancelled.Status);
            Assert.IsNull(cancelled.Save);
            AssertPair(MasterLogLevel.Warning);
            StringAssert.Contains("保存をキャンセルしました", logger.Messages[1]);
        }

        [Test]
        public void Execute_SavedCreated_EndInfoContainsCreated()
        {
            Write(ItemJsonOne);
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.Saved, result.Status);
            Assert.AreEqual(SaveOutcome.Created, result.Save.Outcome);
            AssertPair(MasterLogLevel.Info);
            StringAssert.Contains("保存しました(作成)", logger.Messages[1]);
            StringAssert.Contains("保存先:", logger.Messages[0]);
            StringAssert.Contains(jsonPath, logger.Messages[0]);
        }

        [Test]
        public void Execute_NullRequest_LogsOnlyEndErrorAndThrows()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => pipeline.Execute(null, ProjectTables.Empty));

            Assert.AreEqual("request", exception.ParamName);
            Assert.AreEqual(1, logger.Messages.Count);
            Assert.AreEqual(MasterLogLevel.Error, logger.Levels[0]);
            StringAssert.StartsWith("[終了]", logger.Messages[0]);
            StringAssert.DoesNotContain("[開始]", logger.Messages[0]);
            StringAssert.Contains("理由: 依頼がありません。", logger.Messages[0]);
            Assert.AreEqual(1, EndCount());
        }

        [Test]
        public void Execute_InvalidMasterType_LogsStartAndEndWithReason()
        {
            Write(ItemJsonOne);
            var request = new PipelineRequest(
                null,
                typeof(TestItemRecord),
                jsonPath,
                Asset("Item.asset"),
                PipelineMode.Validate);
            var result = pipeline.Execute(request, ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.InvalidRequest, result.Status);
            AssertPair(MasterLogLevel.Error);
            StringAssert.Contains("理由:", logger.Messages[1]);
            StringAssert.Contains("マスタ型が指定されていません。", logger.Messages[1]);
        }

        [Test]
        public void Execute_UnexpectedError_EndIsErrorAndOnlyTwoLines()
        {
            Write(ItemJsonOne);
            var created = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);
            Assert.AreEqual(SaveOutcome.Created, created.Save.Outcome);

            logger.Clear();
            Write(ItemJsonTwo);
            confirm.Throw = new InvalidOperationException("boom");
            var result = pipeline.Execute(ItemRequest(PipelineMode.ValidateAndSave), ProjectTables.Empty);

            Assert.AreEqual(PipelineStatus.UnexpectedError, result.Status);
            AssertPair(MasterLogLevel.Error);
            StringAssert.Contains("実行できませんでした", logger.Messages[1]);
            StringAssert.Contains("理由:", logger.Messages[1]);
        }

        [Test]
        public void Execute_LoggerThrows_ResultMatchesSilentPipeline()
        {
            Write(ItemJsonOne);
            var request = ItemRequest(PipelineMode.Validate);
            var silent = new MasterImportPipeline(new FakeConfirm()).Execute(request, ProjectTables.Empty);
            var thrown = new MasterImportPipeline(new FakeConfirm(), null, new ThrowingLogger())
                .Execute(request, ProjectTables.Empty);

            Assert.AreEqual(silent.Status, thrown.Status);
            Assert.AreEqual(silent.Message, thrown.Message);
            Assert.AreEqual(silent.CanSave, thrown.CanSave);
            Assert.AreEqual(silent.Validation.ErrorCount, thrown.Validation.ErrorCount);
            Assert.AreEqual(silent.Validation.WarningCount, thrown.Validation.WarningCount);
            Assert.IsNull(silent.Save);
            Assert.IsNull(thrown.Save);
            Assert.AreEqual(PipelineStatus.Validated, thrown.Status);
        }

        void AssertPair(MasterLogLevel endLevel)
        {
            Assert.AreEqual(2, logger.Messages.Count);
            Assert.AreEqual(MasterLogLevel.Info, logger.Levels[0]);
            Assert.AreEqual(endLevel, logger.Levels[1]);
            StringAssert.StartsWith("[開始]", logger.Messages[0]);
            StringAssert.StartsWith("[終了]", logger.Messages[1]);
            StringAssert.DoesNotContain("\n", logger.Messages[1]);
            StringAssert.DoesNotContain("\r", logger.Messages[1]);
            Assert.AreEqual(1, EndCount());
        }

        int EndCount()
        {
            var count = 0;
            for (var i = 0; i < logger.Messages.Count; i++)
            {
                if (logger.Messages[i].StartsWith("[終了]", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
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
            return new PipelineRequest(
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                jsonPath,
                Asset("Item.asset"),
                mode);
        }

        sealed class RecordingLogger : IMasterLogger
        {
            public readonly List<MasterLogLevel> Levels = new List<MasterLogLevel>();
            public readonly List<string> Messages = new List<string>();

            public void Log(MasterLogLevel level, string message)
            {
                Levels.Add(level);
                Messages.Add(message);
            }

            public void Clear()
            {
                Levels.Clear();
                Messages.Clear();
            }
        }

        sealed class ThrowingLogger : IMasterLogger
        {
            public void Log(MasterLogLevel level, string message)
            {
                throw new InvalidOperationException("log failed");
            }
        }

        sealed class FakeConfirm : IOverwriteConfirmation
        {
            public bool Result = true;
            public Exception Throw;

            public bool Confirm(string assetPath, int previousRecordCount, int newRecordCount)
            {
                if (Throw != null)
                {
                    throw Throw;
                }

                return Result;
            }
        }
    }
}
