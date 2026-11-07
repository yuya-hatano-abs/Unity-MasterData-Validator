using UnityEngine;

namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// Unity の Console へ実行ログを出します。
    /// </summary>
    internal sealed class ConsoleMasterLogger : IMasterLogger
    {
        /// <inheritdoc />
        public void Log(MasterLogLevel level, string message)
        {
            var line = "[MasterDataTool] " + (message ?? string.Empty);
            if (level == MasterLogLevel.Warning)
            {
                Debug.LogWarning(line);
                return;
            }

            if (level == MasterLogLevel.Error)
            {
                Debug.LogError(line);
                return;
            }

            Debug.Log(line);
        }
    }
}
