using System;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// インポーターウィンドウの入力状態です。ドメインリロードをまたいで保持します。
    /// </summary>
    [Serializable]
    internal sealed class MasterToolState
    {
        /// <summary>選択中のマスタ型の FullName です。未選択は空文字です。</summary>
        public string SelectedTypeName;

        /// <summary>JSON ファイルのパスです。</summary>
        public string JsonPath;

        /// <summary>保存先アセットのパスです。</summary>
        public string AssetPath;

        /// <summary>JSON 選択ダイアログの初期ディレクトリです。セッション中のみ使います。</summary>
        public string LastJsonDirectory;

        /// <summary>CreateGUI で復元を試みたあと true です。空の入力でも、保存値では上書きしません。</summary>
        public bool SettingsRestored;
    }
}
