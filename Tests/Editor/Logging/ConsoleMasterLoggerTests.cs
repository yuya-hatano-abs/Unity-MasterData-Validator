using MasterDataTool.EditorTools.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ConsoleMasterLogger"/> と <see cref="NullMasterLogger"/> のテストです。
    /// </summary>
    public sealed class ConsoleMasterLoggerTests
    {
        [Test]
        public void Log_Info_WritesPrefixedLog()
        {
            LogAssert.Expect(LogType.Log, "[MasterDataTool] 開始");
            new ConsoleMasterLogger().Log(MasterLogLevel.Info, "開始");
        }

        [Test]
        public void Log_Warning_WritesPrefixedWarning()
        {
            LogAssert.Expect(LogType.Warning, "[MasterDataTool] 注意");
            new ConsoleMasterLogger().Log(MasterLogLevel.Warning, "注意");
        }

        [Test]
        public void Log_Error_WritesPrefixedError()
        {
            LogAssert.Expect(LogType.Error, "[MasterDataTool] 失敗");
            new ConsoleMasterLogger().Log(MasterLogLevel.Error, "失敗");
        }

        [Test]
        public void Log_NullMessage_WritesPrefixOnly()
        {
            LogAssert.Expect(LogType.Log, "[MasterDataTool] ");
            Assert.DoesNotThrow(() => new ConsoleMasterLogger().Log(MasterLogLevel.Info, null));
        }

        [Test]
        public void NullMasterLogger_DoesNotThrowOrLog()
        {
            Assert.DoesNotThrow(() => NullMasterLogger.Instance.Log(MasterLogLevel.Info, "出さない"));
            Assert.DoesNotThrow(() => NullMasterLogger.Instance.Log(MasterLogLevel.Warning, null));
            Assert.DoesNotThrow(() => NullMasterLogger.Instance.Log(MasterLogLevel.Error, "出さない"));
        }
    }
}
