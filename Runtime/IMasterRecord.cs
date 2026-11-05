namespace MasterDataTool.Runtime
{
    /// <summary>
    /// マスタテーブルの 1 行を表すレコードの最小インターフェースです。
    /// </summary>
    public interface IMasterRecord
    {
        /// <summary>ID の種類です。</summary>
        MasterIdKind IdKind { get; }

        /// <summary>
        /// 重複チェックや参照解決に使う正規化キーです。
        /// 比較は <see cref="System.StringComparer.Ordinal"/> 相当です。
        /// </summary>
        string Key { get; }
    }
}
