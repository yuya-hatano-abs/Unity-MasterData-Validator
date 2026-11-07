using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Logging;
using MasterDataTool.EditorTools.Pipeline;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ToggleMasterLogger"/> のテストです。Console には出しません。
    /// </summary>
    public sealed class ToggleMasterLoggerTests
    {
        [Test]
        public void Log_WhenEnabled_ForwardsLevelAndMessage()
        {
            var inner = new RecordingLogger();
            var logger = new ToggleMasterLogger(inner, () => true);

            logger.Log(MasterLogLevel.Warning, "注意");

            Assert.AreEqual(1, inner.Levels.Count);
            Assert.AreEqual(MasterLogLevel.Warning, inner.Levels[0]);
            Assert.AreEqual("注意", inner.Messages[0]);
        }

        [Test]
        public void Log_WhenDisabled_DoesNotCallInner()
        {
            var inner = new RecordingLogger();
            var logger = new ToggleMasterLogger(inner, () => false);

            logger.Log(MasterLogLevel.Error, "失敗");

            Assert.AreEqual(0, inner.Levels.Count);
        }

        [Test]
        public void Log_EnableFlag_ChangesWithoutRecreatingLogger()
        {
            var enabled = true;
            var inner = new RecordingLogger();
            var logger = new ToggleMasterLogger(inner, () => enabled);

            logger.Log(MasterLogLevel.Info, "オン");
            enabled = false;
            logger.Log(MasterLogLevel.Info, "オフ");
            enabled = true;
            logger.Log(MasterLogLevel.Error, "再度オン");

            Assert.AreEqual(2, inner.Messages.Count);
            Assert.AreEqual("オン", inner.Messages[0]);
            Assert.AreEqual(MasterLogLevel.Error, inner.Levels[1]);
            Assert.AreEqual("再度オン", inner.Messages[1]);
        }

        [Test]
        public void Constructor_NullInner_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ToggleMasterLogger(null, () => true));

            Assert.AreEqual("inner", exception.ParamName);
        }

        [Test]
        public void Constructor_NullIsEnabled_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ToggleMasterLogger(new RecordingLogger(), null));

            Assert.AreEqual("isEnabled", exception.ParamName);
        }

        [Test]
        public void Pipeline_Disabled_MatchesEnabledStatusAndRecordsNothing()
        {
            var enabled = true;
            var inner = new RecordingLogger();
            var toggle = new ToggleMasterLogger(inner, () => enabled);
            var pipeline = new MasterImportPipeline(logger: toggle);
            var request = new PipelineRequest(null, null, string.Empty, string.Empty, PipelineMode.Validate);

            var enabledResult = pipeline.Execute(request, ProjectTables.Empty);
            Assert.Greater(inner.Messages.Count, 0);

            inner.Clear();
            enabled = false;
            var disabledResult = pipeline.Execute(request, ProjectTables.Empty);

            Assert.AreEqual(enabledResult.Status, disabledResult.Status);
            Assert.AreEqual(0, inner.Messages.Count);
        }

        sealed class RecordingLogger : IMasterLogger
        {
            public List<MasterLogLevel> Levels { get; } = new List<MasterLogLevel>();

            public List<string> Messages { get; } = new List<string>();

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
    }
}
