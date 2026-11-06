using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MasterDataTool.EditorTools.Importing;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// 生成された C# ソースをファイルに書き込みます。
    /// </summary>
    public static class MasterCodeWriter
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// 絶対ディレクトリに 2 ファイルを書き込みます。
        /// </summary>
        /// <param name="code">生成コードです。</param>
        /// <param name="absoluteDirectory">出力先の絶対ディレクトリです。</param>
        /// <param name="policy">上書き方針です。</param>
        /// <returns>書き込み結果です。</returns>
        public static CodeWriteResult WriteToDirectory(GeneratedCode code, string absoluteDirectory, OverwritePolicy policy)
        {
            var issues = new List<GenerationIssue>();
            var writtenFiles = new List<string>();

            if (code == null)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.SourceError,
                    "生成コードが指定されていません。",
                    null,
                    0));
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            if (string.IsNullOrEmpty(absoluteDirectory))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidOutputPath,
                    "出力ディレクトリが空です。",
                    null,
                    0));
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            try
            {
                Directory.CreateDirectory(absoluteDirectory);
            }
            catch (Exception ex)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.IoError,
                    "ディレクトリを作成できません: " + ex.Message,
                    null,
                    0));
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            var recordPath = Path.Combine(absoluteDirectory, code.RecordFileName);
            var masterPath = Path.Combine(absoluteDirectory, code.MasterFileName);

            if (policy == OverwritePolicy.Fail)
            {
                if (File.Exists(recordPath))
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.FileExists,
                        "ファイルが既に存在します: " + recordPath,
                        null,
                        0));
                }

                if (File.Exists(masterPath))
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.FileExists,
                        "ファイルが既に存在します: " + masterPath,
                        null,
                        0));
                }

                if (issues.Count > 0)
                {
                    return new CodeWriteResult(writtenFiles, issues, succeeded: false);
                }
            }

            if (!TryWriteFile(recordPath, code.RecordSource, writtenFiles, issues))
            {
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            if (!TryWriteFile(masterPath, code.MasterSource, writtenFiles, issues))
            {
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            return new CodeWriteResult(writtenFiles, issues, succeeded: true);
        }

        /// <summary>
        /// Assets 配下のフォルダに 2 ファイルを書き込みます。
        /// </summary>
        /// <param name="code">生成コードです。</param>
        /// <param name="assetFolderPath">Assets/ で始まるプロジェクト相対パスです。</param>
        /// <param name="policy">上書き方針です。</param>
        /// <returns>書き込み結果です。</returns>
        public static CodeWriteResult WriteToAssetFolder(GeneratedCode code, string assetFolderPath, OverwritePolicy policy)
        {
            var issues = new List<GenerationIssue>();
            var writtenFiles = new List<string>();

            if (!TryValidateAssetFolderPath(assetFolderPath, issues))
            {
                return new CodeWriteResult(writtenFiles, issues, succeeded: false);
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var absoluteDirectory = Path.GetFullPath(Path.Combine(projectRoot, assetFolderPath.Replace('/', Path.DirectorySeparatorChar)));

            var result = WriteToDirectory(code, absoluteDirectory, policy);
            if (!result.Succeeded)
            {
                return result;
            }

            AssetDatabase.Refresh();
            return result;
        }

        static bool TryValidateAssetFolderPath(string assetFolderPath, IList<GenerationIssue> issues)
        {
            if (string.IsNullOrEmpty(assetFolderPath))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidOutputPath,
                    "出力パスが空です。",
                    null,
                    0));
                return false;
            }

            if (Path.IsPathRooted(assetFolderPath))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidOutputPath,
                    "出力パスは Assets/ で始まるプロジェクト相対パスである必要があります: " + assetFolderPath,
                    null,
                    0));
                return false;
            }

            if (!assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !string.Equals(assetFolderPath, "Assets", StringComparison.Ordinal))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidOutputPath,
                    "出力パスは Assets/ で始まる必要があります: " + assetFolderPath,
                    null,
                    0));
                return false;
            }

            if (assetFolderPath.IndexOf("Assets/../", StringComparison.Ordinal) >= 0 ||
                assetFolderPath.IndexOf("Assets/..\\", StringComparison.Ordinal) >= 0)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidOutputPath,
                    "出力パスに Assets/../ を含めることはできません: " + assetFolderPath,
                    null,
                    0));
                return false;
            }

            return true;
        }

        static bool TryWriteFile(string path, string contents, IList<string> writtenFiles, IList<GenerationIssue> issues)
        {
            try
            {
                File.WriteAllText(path, contents, Utf8NoBom);
                writtenFiles.Add(path);
                return true;
            }
            catch (Exception ex)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.IoError,
                    "ファイルを書き込めません: " + path + " (" + ex.Message + ")",
                    null,
                    0));
                return false;
            }
        }
    }
}
