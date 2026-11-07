using UnityEditor;

namespace MasterDataTool.EditorTools.Settings
{
    /// <summary>
    /// 文字列と bool の設定を、キー単位で読み書きします。
    /// </summary>
    internal interface ISettingsStore
    {
        /// <summary>
        /// 文字列を読みます。キーが無いときだけ false です。空文字は存在する値です。
        /// </summary>
        /// <param name="key">設定キーです。</param>
        /// <param name="value">見つかった値です。無いときは null です。</param>
        /// <returns>キーがあるとき true です。</returns>
        bool TryGetString(string key, out string value);

        /// <summary>
        /// 文字列を書き込みます。
        /// </summary>
        /// <param name="key">設定キーです。</param>
        /// <param name="value">書き込む値です。null は空文字として保存します。</param>
        void SetString(string key, string value);

        /// <summary>
        /// bool を読みます。キーが無いときだけ false です。
        /// </summary>
        /// <param name="key">設定キーです。</param>
        /// <param name="value">見つかった値です。無いときは false です。</param>
        /// <returns>キーがあるとき true です。</returns>
        bool TryGetBool(string key, out bool value);

        /// <summary>
        /// bool を書き込みます。
        /// </summary>
        /// <param name="key">設定キーです。</param>
        /// <param name="value">書き込む値です。</param>
        void SetBool(string key, bool value);
    }

    /// <summary>
    /// <see cref="EditorPrefs"/> へ設定を保存します。
    /// </summary>
    internal sealed class EditorPrefsSettingsStore : ISettingsStore
    {
        /// <inheritdoc />
        public bool TryGetString(string key, out string value)
        {
            if (!EditorPrefs.HasKey(key))
            {
                value = null;
                return false;
            }

            value = EditorPrefs.GetString(key);
            return true;
        }

        /// <inheritdoc />
        public void SetString(string key, string value)
        {
            EditorPrefs.SetString(key, value ?? string.Empty);
        }

        /// <inheritdoc />
        public bool TryGetBool(string key, out bool value)
        {
            if (!EditorPrefs.HasKey(key))
            {
                value = false;
                return false;
            }

            value = EditorPrefs.GetBool(key);
            return true;
        }

        /// <inheritdoc />
        public void SetBool(string key, bool value)
        {
            EditorPrefs.SetBool(key, value);
        }
    }
}
