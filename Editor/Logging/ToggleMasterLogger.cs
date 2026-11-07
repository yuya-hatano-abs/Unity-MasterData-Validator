using System;

namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// 呼び出しごとに有効かを見て、有効なときだけ内側のロガーへ渡します。
    /// </summary>
    internal sealed class ToggleMasterLogger : IMasterLogger
    {
        readonly IMasterLogger inner;
        readonly Func<bool> isEnabled;

        /// <summary>
        /// ロガーを初期化します。
        /// </summary>
        /// <param name="inner">有効なときに呼び出すロガーです。</param>
        /// <param name="isEnabled">記録してよいとき true を返します。</param>
        public ToggleMasterLogger(IMasterLogger inner, Func<bool> isEnabled)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (isEnabled == null)
            {
                throw new ArgumentNullException(nameof(isEnabled));
            }

            this.inner = inner;
            this.isEnabled = isEnabled;
        }

        /// <inheritdoc />
        public void Log(MasterLogLevel level, string message)
        {
            if (isEnabled())
            {
                inner.Log(level, message);
            }
        }
    }
}
