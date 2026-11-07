using System.Collections.Generic;
using MasterDataTool.EditorTools.Diff;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 差分レポートを、色を使わない文字列にします。
    /// </summary>
    internal static class MasterDiffFormatter
    {
        /// <summary>追加・削除・変更それぞれに出す件数の上限です。</summary>
        internal const int MaxEntriesPerGroup = 20;

        /// <summary>1 レコードに出すフィールド数の上限です。</summary>
        internal const int MaxFieldsPerRecord = 5;

        /// <summary>値の文字数の上限です。超えた分は「…」にします。</summary>
        internal const int MaxValueCharacters = 40;

        /// <summary>コレクションの要素数の上限です。</summary>
        internal const int MaxCollectionElements = 5;

        /// <summary>
        /// 折りたたみ時にも見える 1 行です。
        /// </summary>
        /// <param name="report">差分です。</param>
        /// <returns>要約です。</returns>
        public static string FormatSummary(MasterDiffReport report)
        {
            if (report == null)
            {
                throw new System.ArgumentNullException(nameof(report));
            }

            if (report.Status == MasterDiffStatus.Unavailable)
            {
                if (string.IsNullOrEmpty(report.Reason))
                {
                    return "差分を計算できません";
                }

                return "差分を計算できません。" + report.Reason;
            }

            var added = report.AddedKeys.Count;
            var removed = report.RemovedKeys.Count;
            var changed = report.Changes.Count;
            if (added == 0 && removed == 0 && changed == 0)
            {
                if (report.OrderChanged)
                {
                    return "ID・フィールドの変更なし(並び順のみ変更)";
                }

                if (report.SerializedRecordsEqual)
                {
                    return "変更なし";
                }

                return "ID・フィールドの差分は検出されませんでした(保存データには差異があります)";
            }

            var summary = "追加 " + added + " / 削除 " + removed + " / 変更 " + changed +
                          " / 変更なし " + report.UnchangedCount;
            if (report.OrderChanged)
            {
                summary += " (並び順の変更あり)";
            }

            return summary;
        }

        /// <summary>
        /// 開いたときに出す行です。追加、削除、変更の順です。
        /// </summary>
        /// <param name="report">差分です。</param>
        /// <returns>詳細行です。</returns>
        public static IReadOnlyList<string> FormatDetails(MasterDiffReport report)
        {
            if (report == null)
            {
                throw new System.ArgumentNullException(nameof(report));
            }

            var lines = new List<string>();
            if (report.Status == MasterDiffStatus.Unavailable)
            {
                if (!string.IsNullOrEmpty(report.Reason))
                {
                    lines.Add(report.Reason);
                }

                return lines;
            }

            AppendKeys(lines, "+", report.AddedKeys);
            AppendKeys(lines, "-", report.RemovedKeys);
            AppendChanges(lines, report.Changes);
            return lines;
        }

        static void AppendKeys(List<string> lines, string sign, IReadOnlyList<string> keys)
        {
            var shown = keys.Count < MaxEntriesPerGroup ? keys.Count : MaxEntriesPerGroup;
            for (var i = 0; i < shown; i++)
            {
                lines.Add(sign + " " + keys[i]);
            }

            var rest = keys.Count - shown;
            if (rest > 0)
            {
                lines.Add("…ほか " + rest + " 件");
            }
        }

        static void AppendChanges(List<string> lines, IReadOnlyList<MasterRecordChange> changes)
        {
            var shown = changes.Count < MaxEntriesPerGroup ? changes.Count : MaxEntriesPerGroup;
            for (var i = 0; i < shown; i++)
            {
                var change = changes[i];
                lines.Add("~ " + change.Key);
                var fields = change.Fields;
                var fieldShown = fields.Count < MaxFieldsPerRecord ? fields.Count : MaxFieldsPerRecord;
                for (var f = 0; f < fieldShown; f++)
                {
                    var field = fields[f];
                    lines.Add("  " + field.Name + ": " + FormatValue(field.OldValue) + " → " + FormatValue(field.NewValue));
                }

                var restFields = fields.Count - fieldShown;
                if (restFields > 0)
                {
                    lines.Add("  …ほか " + restFields + " フィールド");
                }
            }

            var rest = changes.Count - shown;
            if (rest > 0)
            {
                lines.Add("…ほか " + rest + " 件");
            }
        }

        static string FormatValue(MasterDiffValue value)
        {
            if (value == null)
            {
                return "(null)";
            }

            if (!value.IsCollection)
            {
                return Truncate(value.Text);
            }

            var elements = value.Elements;
            var shown = elements.Count < MaxCollectionElements ? elements.Count : MaxCollectionElements;
            var text = "[";
            for (var i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    text += ", ";
                }

                text += Truncate(elements[i]);
            }

            var rest = elements.Count - shown;
            if (rest > 0)
            {
                text += "…(+" + rest + ")";
            }

            text += "]";
            return text;
        }

        static string Truncate(string text)
        {
            if (text == null)
            {
                return "(null)";
            }

            if (text.Length <= MaxValueCharacters)
            {
                return text;
            }

            return text.Substring(0, MaxValueCharacters) + "…";
        }
    }
}
