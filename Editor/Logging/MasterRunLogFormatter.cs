using System;
using System.Collections.Generic;
using System.Globalization;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Pipeline;

namespace MasterDataTool.EditorTools.Logging
{
    /// <summary>
    /// 実行 1 回の開始行と終了行を組み立てます。出力先には依存しません。
    /// </summary>
    internal static class MasterRunLogFormatter
    {
        /// <summary>
        /// 開始の 1 行を作ります。
        /// </summary>
        /// <param name="request">依頼です。</param>
        /// <returns>接頭辞のない開始行です。</returns>
        public static string FormatStart(PipelineRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var parts = new List<string>();
            parts.Add("モード: " + ModeLabel(request.Mode));
            if (request.MasterType != null)
            {
                parts.Add("マスタ: " + request.MasterType.Name);
            }

            if (!string.IsNullOrEmpty(request.JsonPath))
            {
                parts.Add("JSON: " + request.JsonPath);
            }

            if (request.Mode == PipelineMode.ValidateAndSave && !string.IsNullOrEmpty(request.AssetPath))
            {
                parts.Add("保存先: " + request.AssetPath);
            }

            return "[開始] " + Join(parts);
        }

        /// <summary>
        /// 終了の 1 行を作ります。
        /// </summary>
        /// <param name="result">実行結果です。</param>
        /// <param name="elapsedMilliseconds">所要時間です。負の値は 0 として出します。</param>
        /// <returns>接頭辞のない終了行です。改行は含みません。</returns>
        public static string FormatEnd(PipelineResult result, long elapsedMilliseconds)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (elapsedMilliseconds < 0)
            {
                elapsedMilliseconds = 0;
            }

            var parts = new List<string>();
            parts.Add(StatusPhrase(result));
            var reason = CollapseLines(result.Message);
            if (!string.IsNullOrEmpty(reason))
            {
                parts.Add("理由: " + reason);
            }

            if (result.Table != null)
            {
                parts.Add("レコード: " + result.Table.Records.Count + " 件");
            }

            parts.Add("エラー: " + result.Validation.ErrorCount);
            parts.Add("警告: " + result.Validation.WarningCount);
            parts.Add("所要時間: " + elapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms");
            return "[終了] " + Join(parts);
        }

        /// <summary>
        /// 終了行の重大度を決めます。Error、Warning、Info の順で判定します。
        /// </summary>
        /// <param name="result">実行結果です。</param>
        /// <returns>終了行の重大度です。</returns>
        public static MasterLogLevel GetEndLevel(PipelineResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Status == PipelineStatus.ValidationFailed ||
                result.Status == PipelineStatus.ImportFailed ||
                result.Status == PipelineStatus.SaveFailed ||
                result.Status == PipelineStatus.InvalidRequest ||
                result.Status == PipelineStatus.UnexpectedError)
            {
                return MasterLogLevel.Error;
            }

            if (result.Status == PipelineStatus.SaveCancelled || result.Validation.WarningCount > 0)
            {
                return MasterLogLevel.Warning;
            }

            return MasterLogLevel.Info;
        }

        static string ModeLabel(PipelineMode mode)
        {
            if (mode == PipelineMode.Validate)
            {
                return "検証";
            }

            if (mode == PipelineMode.ValidateAndSave)
            {
                return "検証して保存";
            }

            return mode.ToString();
        }

        static string StatusPhrase(PipelineResult result)
        {
            switch (result.Status)
            {
                case PipelineStatus.InvalidRequest:
                    return "依頼が不正です";
                case PipelineStatus.ImportFailed:
                    return "インポートに失敗しました";
                case PipelineStatus.ValidationFailed:
                    return "検証に失敗しました";
                case PipelineStatus.Validated:
                    return "検証しました";
                case PipelineStatus.Saved:
                    return SavedPhrase(result.Save);
                case PipelineStatus.SaveCancelled:
                    return "保存をキャンセルしました";
                case PipelineStatus.SaveFailed:
                    return "保存に失敗しました";
                case PipelineStatus.UnexpectedError:
                    return "実行できませんでした";
                default:
                    return result.Status.ToString();
            }
        }

        static string SavedPhrase(PipelineSaveInfo save)
        {
            if (save == null)
            {
                return "保存しました";
            }

            if (save.Outcome == SaveOutcome.Created)
            {
                return "保存しました(作成)";
            }

            if (save.Outcome == SaveOutcome.Updated)
            {
                return "保存しました(更新)";
            }

            if (save.Outcome == SaveOutcome.Unchanged)
            {
                return "内容が同じため、書き込みませんでした";
            }

            return "保存しました";
        }

        static string CollapseLines(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            return JoinWith(new List<string>(lines), " ");
        }

        static string Join(List<string> parts)
        {
            return JoinWith(parts, " ・ ");
        }

        static string JoinWith(List<string> parts, string separator)
        {
            var text = string.Empty;
            for (var i = 0; i < parts.Count; i++)
            {
                if (i > 0)
                {
                    text += separator;
                }

                text += parts[i];
            }

            return text;
        }
    }
}
