using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// ファイル書き込みの結果です。
    /// </summary>
    public sealed class CodeWriteResult
    {
        /// <summary>
        /// 書き込み結果を初期化します。
        /// </summary>
        /// <param name="writtenFiles">書き込んだファイルのフルパス一覧です。</param>
        /// <param name="issues">問題一覧です。</param>
        /// <param name="succeeded">書き込みが成功したかどうかです。</param>
        public CodeWriteResult(IReadOnlyList<string> writtenFiles, IReadOnlyList<GenerationIssue> issues, bool succeeded)
        {
            WrittenFiles = writtenFiles;
            Issues = issues;
            Succeeded = succeeded;
        }

        /// <summary>書き込んだファイルのフルパス一覧です。</summary>
        public IReadOnlyList<string> WrittenFiles { get; }

        /// <summary>問題一覧です。</summary>
        public IReadOnlyList<GenerationIssue> Issues { get; }

        /// <summary>書き込みが成功したかどうかです。</summary>
        public bool Succeeded { get; }
    }
}
