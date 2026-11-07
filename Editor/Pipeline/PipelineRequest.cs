using System;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// インポートパイプラインへの 1 回の依頼です。
    /// </summary>
    internal sealed class PipelineRequest
    {
        /// <summary>
        /// 依頼を初期化します。型の妥当性は実行時に検査します。
        /// </summary>
        /// <param name="masterType">マスタ型です。</param>
        /// <param name="recordType">レコード型です。</param>
        /// <param name="jsonPath">JSON ファイルのパスです。</param>
        /// <param name="assetPath">保存先アセットのパスです。</param>
        /// <param name="mode">実行モードです。</param>
        public PipelineRequest(Type masterType, Type recordType, string jsonPath, string assetPath, PipelineMode mode)
        {
            MasterType = masterType;
            RecordType = recordType;
            JsonPath = jsonPath;
            AssetPath = assetPath;
            Mode = mode;
        }

        /// <summary>マスタ型です。</summary>
        public Type MasterType { get; }

        /// <summary>レコード型です。</summary>
        public Type RecordType { get; }

        /// <summary>JSON ファイルのパスです。</summary>
        public string JsonPath { get; }

        /// <summary>保存先アセットのパスです。</summary>
        public string AssetPath { get; }

        /// <summary>実行モードです。</summary>
        public PipelineMode Mode { get; }
    }
}
