using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// スキーマ推論の結果です。
    /// </summary>
    public sealed class SchemaInferenceResult
    {
        /// <summary>
        /// 推論結果を初期化します。
        /// </summary>
        /// <param name="schema">推論されたスキーマです。失敗時は null です。</param>
        /// <param name="issues">問題一覧です。</param>
        /// <param name="succeeded">推論が成功したかどうかです。</param>
        public SchemaInferenceResult(MasterSchema schema, IReadOnlyList<GenerationIssue> issues, bool succeeded)
        {
            Schema = schema;
            Issues = issues;
            Succeeded = succeeded;
        }

        /// <summary>推論されたスキーマです。</summary>
        public MasterSchema Schema { get; }

        /// <summary>問題一覧です。</summary>
        public IReadOnlyList<GenerationIssue> Issues { get; }

        /// <summary>推論が成功したかどうかです。</summary>
        public bool Succeeded { get; }
    }
}
