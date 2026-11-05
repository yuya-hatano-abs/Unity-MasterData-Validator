namespace MasterDataTool.EditorTools
{
    /// <summary>
    /// マスタデータ管理ツール全体で共有する定数を保持します。
    /// </summary>
    internal static class MasterDataToolInfo
    {
        /// <summary>ツールの表示名です。</summary>
        internal const string ToolName = "Master Data Tool";

        /// <summary>ツールのバージョン文字列です。</summary>
        internal const string Version = "0.1.0";

        /// <summary>Unity メニューのルートパスです。</summary>
        internal const string MenuRoot = "Tools/Master Data Tool";

        /// <summary>EditorPrefs 等で使用する設定キーの接頭辞です。</summary>
        internal const string SettingsKeyPrefix = "MasterDataTool.";
    }
}
