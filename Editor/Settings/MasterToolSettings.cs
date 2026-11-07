using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MasterDataTool.EditorTools.UI;
using UnityEngine;

namespace MasterDataTool.EditorTools.Settings
{
    /// <summary>
    /// インポーターの入力と、ログのオン/オフをプロジェクトごとに保存します。
    /// </summary>
    internal sealed class MasterToolSettings
    {
        const string JsonPathSuffix = ".JsonPath";
        const string AssetPathSuffix = ".AssetPath";
        const string MasterTypeSuffix = ".MasterType";
        const string LogToConsoleSuffix = ".LogToConsole";

        readonly ISettingsStore store;
        readonly string projectId;

        /// <summary>
        /// 設定を初期化します。
        /// </summary>
        /// <param name="store">読み書き先です。</param>
        /// <param name="projectId">プロジェクト識別子です。キーに含めます。</param>
        public MasterToolSettings(ISettingsStore store, string projectId)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("プロジェクト識別子が空です。", nameof(projectId));
            }

            this.store = store;
            this.projectId = projectId;
        }

        /// <summary>
        /// Console へログを出すかです。キーが無いときは true です。同じ値の代入は書き込みません。
        /// </summary>
        public bool LogToConsole
        {
            get
            {
                if (!store.TryGetBool(Key(LogToConsoleSuffix), out var value))
                {
                    return true;
                }

                return value;
            }

            set
            {
                if (LogToConsole == value)
                {
                    return;
                }

                store.SetBool(Key(LogToConsoleSuffix), value);
            }
        }

        /// <summary>
        /// データパスから、プロジェクト識別子を作ります。同じ入力は常に同じ 16 文字です。
        /// </summary>
        /// <param name="dataPath">プロジェクトの Assets フォルダのパスです。</param>
        /// <returns>SHA-256 の 16 進文字列の先頭 16 文字です。</returns>
        public static string ComputeProjectId(string dataPath)
        {
            if (dataPath == null)
            {
                throw new ArgumentNullException(nameof(dataPath));
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(dataPath));
                var builder = new StringBuilder(16);
                for (var i = 0; i < 8; i++)
                {
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// このエディタの <see cref="EditorPrefsSettingsStore"/> と、現在のプロジェクト識別子で設定を作ります。
        /// </summary>
        /// <returns>本番用の設定です。</returns>
        public static MasterToolSettings CreateDefault()
        {
            return new MasterToolSettings(new EditorPrefsSettingsStore(), ComputeProjectId(Application.dataPath));
        }

        /// <summary>
        /// マスタ型、JSON パス、保存先を保存します。同じ値は書き直さず、空文字も保存します。
        /// </summary>
        /// <param name="state">保存する状態です。</param>
        public void Save(MasterToolState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            WriteIfChanged(Key(JsonPathSuffix), state.JsonPath);
            WriteIfChanged(Key(AssetPathSuffix), state.AssetPath);
            WriteIfChanged(Key(MasterTypeSuffix), state.SelectedTypeName);
        }

        /// <summary>
        /// 保存されている項目だけを、状態のフィールドへ直接代入します。
        /// </summary>
        /// <param name="state">復元先です。</param>
        /// <param name="typeExists">マスタ型の FullName が見つかるとき true を返します。</param>
        public void LoadInto(MasterToolState state, Func<string, bool> typeExists)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (typeExists == null)
            {
                throw new ArgumentNullException(nameof(typeExists));
            }

            if (store.TryGetString(Key(JsonPathSuffix), out var jsonPath))
            {
                state.JsonPath = jsonPath ?? string.Empty;
            }

            if (store.TryGetString(Key(AssetPathSuffix), out var assetPath))
            {
                state.AssetPath = assetPath ?? string.Empty;
            }

            if (store.TryGetString(Key(MasterTypeSuffix), out var masterType))
            {
                var name = masterType ?? string.Empty;
                state.SelectedTypeName = typeExists(name) ? name : string.Empty;
            }
        }

        /// <summary>
        /// まだ復元していない状態だけ、保存値を読み込みます。
        /// </summary>
        /// <param name="state">復元先です。</param>
        /// <param name="typeExists">マスタ型の FullName が見つかるとき true を返します。</param>
        public void RestoreIfNeeded(MasterToolState state, Func<string, bool> typeExists)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.SettingsRestored)
            {
                return;
            }

            LoadInto(state, typeExists);
            state.SettingsRestored = true;
        }

        void WriteIfChanged(string key, string value)
        {
            var text = value ?? string.Empty;
            if (store.TryGetString(key, out var current) && string.Equals(current, text, StringComparison.Ordinal))
            {
                return;
            }

            store.SetString(key, text);
        }

        string Key(string suffix)
        {
            return MasterDataToolInfo.SettingsKeyPrefix + projectId + suffix;
        }
    }
}
