using System.Collections.Generic;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// パイプラインの結果を、画面に出す行へ変換します。
    /// </summary>
    internal static class PipelineResultFormatter
    {
        /// <summary>
        /// 結果を行に変換します。
        /// </summary>
        /// <param name="result">パイプラインの結果です。</param>
        /// <param name="maxIssueLines">問題の行の上限です。超えた分は 1 行にまとめます。</param>
        /// <returns>サマリー、保存結果、問題の順の行です。</returns>
        public static IReadOnlyList<ResultLine> Format(PipelineResult result, int maxIssueLines = 200)
        {
            var lines = new List<ResultLine>();
            AppendHeader(lines, result);
            if (result == null)
            {
                return lines;
            }

            if (result.Status != PipelineStatus.InvalidRequest &&
                result.Status != PipelineStatus.UnexpectedError)
            {
                var validation = result.Validation ?? ValidationResult.Empty;
                AppendIssueLines(lines, validation, maxIssueLines);
            }

            return lines;
        }

        /// <summary>
        /// サマリーと保存結果だけを行にします。問題の行は含めません。
        /// </summary>
        /// <param name="result">パイプラインの結果です。</param>
        /// <returns>サマリーと、保存した場合はその行です。</returns>
        public static IReadOnlyList<ResultLine> FormatHeader(PipelineResult result)
        {
            var lines = new List<ResultLine>();
            AppendHeader(lines, result);
            return lines;
        }

        static void AppendHeader(List<ResultLine> lines, PipelineResult result)
        {
            if (result == null)
            {
                lines.Add(new ResultLine(ResultLineSeverity.Error, "実行できませんでした: 結果がありません。"));
                return;
            }

            var validation = result.Validation ?? ValidationResult.Empty;
            var errorCount = validation.ErrorCount;
            var warningCount = validation.WarningCount;
            var recordCount = result.Table == null ? 0 : result.Table.Records.Count;

            switch (result.Status)
            {
                case PipelineStatus.InvalidRequest:
                case PipelineStatus.UnexpectedError:
                    lines.Add(new ResultLine(
                        ResultLineSeverity.Error,
                        "実行できませんでした: " + result.Message));
                    break;
                case PipelineStatus.ImportFailed:
                    lines.Add(new ResultLine(
                        ResultLineSeverity.Error,
                        "インポートに失敗しました。(エラー " + errorCount + " 件、警告 " + warningCount + " 件)"));
                    break;
                case PipelineStatus.ValidationFailed:
                    lines.Add(new ResultLine(
                        ResultLineSeverity.Error,
                        "検証しました: エラー " + errorCount + " 件、警告 " + warningCount +
                        " 件。エラーがあるため、保存できません。(レコード " + recordCount + " 件)"));
                    break;
                case PipelineStatus.Validated:
                    lines.Add(new ResultLine(
                        warningCount > 0 ? ResultLineSeverity.Warning : ResultLineSeverity.Info,
                        "検証しました: エラー 0 件、警告 " + warningCount +
                        " 件。保存できます。(レコード " + recordCount + " 件)"));
                    break;
                case PipelineStatus.Saved:
                case PipelineStatus.SaveCancelled:
                case PipelineStatus.SaveFailed:
                    lines.Add(new ResultLine(
                        warningCount > 0 ? ResultLineSeverity.Warning : ResultLineSeverity.Info,
                        "検証しました: エラー 0 件、警告 " + warningCount + " 件。(レコード " + recordCount + " 件)"));
                    AppendSaveLines(lines, result);
                    break;
            }
        }

        static void AppendSaveLines(List<ResultLine> lines, PipelineResult result)
        {
            if (result.Status == PipelineStatus.SaveCancelled)
            {
                lines.Add(new ResultLine(ResultLineSeverity.Warning, "保存をキャンセルしました。"));
                return;
            }

            if (result.Status == PipelineStatus.SaveFailed)
            {
                lines.Add(new ResultLine(ResultLineSeverity.Error, "保存に失敗しました。"));
                if (result.Save == null)
                {
                    return;
                }

                var issues = result.Save.Issues;
                for (var i = 0; i < issues.Count; i++)
                {
                    lines.Add(new ResultLine(ResultLineSeverity.Error, issues[i].Message));
                }

                return;
            }

            if (result.Save == null)
            {
                return;
            }

            var save = result.Save;
            if (save.Outcome == MasterDataTool.EditorTools.Importing.SaveOutcome.Created)
            {
                lines.Add(new ResultLine(
                    ResultLineSeverity.Info,
                    "保存しました(新規作成): " + save.AssetPath + "(" + save.NewRecordCount + " 件)"));
            }
            else if (save.Outcome == MasterDataTool.EditorTools.Importing.SaveOutcome.Updated)
            {
                lines.Add(new ResultLine(
                    ResultLineSeverity.Info,
                    "保存しました(更新): " + save.AssetPath + "(" + save.PreviousRecordCount + " → " +
                    save.NewRecordCount + " 件)"));
            }
            else if (save.Outcome == MasterDataTool.EditorTools.Importing.SaveOutcome.Unchanged)
            {
                lines.Add(new ResultLine(
                    ResultLineSeverity.Info,
                    "内容が同じため、書き込みませんでした: " + save.AssetPath));
            }
        }

        static void AppendIssueLines(List<ResultLine> lines, ValidationResult validation, int maxIssueLines)
        {
            var issues = validation.Issues;
            var limit = maxIssueLines < 0 ? 0 : maxIssueLines;
            var shown = issues.Count < limit ? issues.Count : limit;
            for (var i = 0; i < shown; i++)
            {
                var issue = issues[i];
                var isError = issue.Severity == ValidationSeverity.Error;
                lines.Add(new ResultLine(
                    isError ? ResultLineSeverity.Error : ResultLineSeverity.Warning,
                    (isError ? "[エラー] " : "[警告] ") + issue.Message));
            }

            var rest = issues.Count - shown;
            if (rest > 0)
            {
                lines.Add(new ResultLine(ResultLineSeverity.Info, "…ほか " + rest + " 件"));
            }
        }
    }
}
