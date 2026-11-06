using System.Collections.Generic;
using System.Text.RegularExpressions;
using MasterDataTool.EditorTools.Importing;

namespace MasterDataTool.EditorTools.Generation
{
    /// <summary>
    /// スキーマから C# ソースを生成します。
    /// </summary>
    public static class MasterCodeGenerator
    {
        static readonly Regex MasterNamePattern = new Regex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant);
        static readonly Regex NamespaceSegmentPattern = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// スキーマとオプションから C# ソースを生成します。
        /// </summary>
        /// <param name="schema">スキーマです。</param>
        /// <param name="options">生成オプションです。</param>
        /// <returns>生成結果です。</returns>
        public static CodeGenerationResult Generate(MasterSchema schema, MasterCodeOptions options)
        {
            var issues = new List<GenerationIssue>();

            if (schema == null)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.SourceError,
                    "スキーマが指定されていません。",
                    null,
                    0));
                return new CodeGenerationResult(null, issues, succeeded: false);
            }

            if (!ValidateOptions(options, issues))
            {
                return new CodeGenerationResult(null, issues, succeeded: false);
            }

            var recordClassName = options.MasterName + "Record";
            for (var i = 0; i < schema.Fields.Count; i++)
            {
                var field = schema.Fields[i];
                if (string.Equals(field.PropertyName, recordClassName, System.StringComparison.Ordinal))
                {
                    issues.Add(new GenerationIssue(
                        ImportSeverity.Error,
                        GenerationIssueKind.ReservedFieldName,
                        "キー \"" + field.JsonName + "\" は、レコード型のクラス名 \"" + recordClassName + "\" と衝突します。",
                        field.JsonName,
                        0));
                }
            }

            if (HasError(issues))
            {
                return new CodeGenerationResult(null, issues, succeeded: false);
            }

            var recordSource = CodeTemplates.BuildRecordSource(options, schema);
            var masterSource = CodeTemplates.BuildMasterSource(options);
            var code = new GeneratedCode(
                options.MasterName + "Record.cs",
                recordSource,
                options.MasterName + "Master.cs",
                masterSource,
                recordClassName,
                options.MasterName + "Master");

            return new CodeGenerationResult(code, issues, succeeded: true);
        }

        /// <summary>
        /// JSON から推論して C# ソースを生成します。
        /// </summary>
        /// <param name="json">JSON 文字列です。</param>
        /// <param name="options">生成オプションです。</param>
        /// <returns>生成結果です。</returns>
        public static CodeGenerationResult GenerateFromJson(string json, MasterCodeOptions options)
        {
            var inference = MasterSchemaInferrer.Infer(json);
            var issues = new List<GenerationIssue>(inference.Issues);

            if (!inference.Succeeded || inference.Schema == null)
            {
                return new CodeGenerationResult(null, issues, succeeded: false);
            }

            var generation = Generate(inference.Schema, options);
            for (var i = 0; i < generation.Issues.Count; i++)
            {
                issues.Add(generation.Issues[i]);
            }

            if (!generation.Succeeded || generation.Code == null)
            {
                return new CodeGenerationResult(null, issues, succeeded: false);
            }

            return new CodeGenerationResult(generation.Code, issues, succeeded: true);
        }

        static bool ValidateOptions(MasterCodeOptions options, IList<GenerationIssue> issues)
        {
            if (options == null)
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidMasterName,
                    "マスタ名が指定されていません。",
                    null,
                    0));
                return false;
            }

            var valid = true;
            if (string.IsNullOrEmpty(options.MasterName) ||
                !MasterNamePattern.IsMatch(options.MasterName) ||
                CSharpKeywords.IsKeyword(options.MasterName))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidMasterName,
                    "マスタ名 \"" + (options.MasterName ?? string.Empty) + "\" は不正です。",
                    null,
                    0));
                valid = false;
            }

            if (string.IsNullOrEmpty(options.Namespace))
            {
                issues.Add(new GenerationIssue(
                    ImportSeverity.Error,
                    GenerationIssueKind.InvalidNamespace,
                    "名前空間が空です。",
                    null,
                    0));
                valid = false;
            }
            else
            {
                var segments = options.Namespace.Split('.');
                for (var i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i];
                    if (string.IsNullOrEmpty(segment) ||
                        !NamespaceSegmentPattern.IsMatch(segment) ||
                        CSharpKeywords.IsKeyword(segment))
                    {
                        issues.Add(new GenerationIssue(
                            ImportSeverity.Error,
                            GenerationIssueKind.InvalidNamespace,
                            "名前空間 \"" + options.Namespace + "\" は不正です。",
                            null,
                            0));
                        valid = false;
                        break;
                    }
                }
            }

            return valid;
        }

        static bool HasError(IList<GenerationIssue> issues)
        {
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == ImportSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
