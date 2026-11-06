using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// コード生成の結果です。
    /// </summary>
    public sealed class CodeGenerationResult
    {
        /// <summary>
        /// 生成結果を初期化します。
        /// </summary>
        /// <param name="code">生成されたコードです。失敗時は null です。</param>
        /// <param name="issues">問題一覧です。</param>
        /// <param name="succeeded">生成が成功したかどうかです。</param>
        public CodeGenerationResult(GeneratedCode code, IReadOnlyList<GenerationIssue> issues, bool succeeded)
        {
            Code = code;
            Issues = issues;
            Succeeded = succeeded;
        }

        /// <summary>生成されたコードです。</summary>
        public GeneratedCode Code { get; }

        /// <summary>問題一覧です。</summary>
        public IReadOnlyList<GenerationIssue> Issues { get; }

        /// <summary>生成が成功したかどうかです。</summary>
        public bool Succeeded { get; }
    }
}
