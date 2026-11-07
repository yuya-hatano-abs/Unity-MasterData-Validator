using System.Collections.Generic;

namespace MasterDataTool.EditorTools.Diff
{
    /// <summary>
    /// 差分をフィールド単位まで計算できたかです。
    /// </summary>
    internal enum MasterDiffStatus
    {
        /// <summary>追加・削除・変更を計算できました。</summary>
        Available,

        /// <summary>キーが突き合わせできないため、件数は出していません。</summary>
        Unavailable,
    }

    /// <summary>
    /// フィールド値の表示用テキストです。件数の上限はかけません。
    /// </summary>
    internal sealed class MasterDiffValue
    {
        MasterDiffValue(bool isCollection, string text, IReadOnlyList<string> elements)
        {
            IsCollection = isCollection;
            Text = text ?? string.Empty;
            Elements = elements ?? new string[0];
        }

        /// <summary>単一の値です。</summary>
        /// <param name="text">表示テキストです。</param>
        /// <returns>値です。</returns>
        public static MasterDiffValue Scalar(string text)
        {
            return new MasterDiffValue(false, text, new string[0]);
        }

        /// <summary>配列または List の要素です。</summary>
        /// <param name="elements">要素の表示テキストです。</param>
        /// <returns>値です。</returns>
        public static MasterDiffValue Collection(IReadOnlyList<string> elements)
        {
            return new MasterDiffValue(true, string.Empty, Copy(elements));
        }

        /// <summary>配列または List のとき true です。</summary>
        public bool IsCollection { get; }

        /// <summary>単一値の表示テキストです。</summary>
        public string Text { get; }

        /// <summary>コレクションの要素です。</summary>
        public IReadOnlyList<string> Elements { get; }

        static IReadOnlyList<string> Copy(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return new string[0];
            }

            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i] ?? string.Empty;
            }

            return copy;
        }
    }

    /// <summary>
    /// 1 フィールドの旧値と新値です。
    /// </summary>
    internal sealed class MasterFieldChange
    {
        /// <summary>
        /// フィールドの変更を作ります。
        /// </summary>
        /// <param name="name">フィールド名です。</param>
        /// <param name="oldValue">既存の値です。</param>
        /// <param name="newValue">新しい値です。</param>
        public MasterFieldChange(string name, MasterDiffValue oldValue, MasterDiffValue newValue)
        {
            Name = name ?? string.Empty;
            OldValue = oldValue ?? MasterDiffValue.Scalar("(null)");
            NewValue = newValue ?? MasterDiffValue.Scalar("(null)");
        }

        /// <summary>フィールド名です。</summary>
        public string Name { get; }

        /// <summary>既存の値です。</summary>
        public MasterDiffValue OldValue { get; }

        /// <summary>新しい値です。</summary>
        public MasterDiffValue NewValue { get; }
    }

    /// <summary>
    /// 1 レコードのフィールド差分です。
    /// </summary>
    internal sealed class MasterRecordChange
    {
        /// <summary>
        /// レコードの変更を作ります。
        /// </summary>
        /// <param name="key">正規化キーです。</param>
        /// <param name="fields">変わったフィールドです。</param>
        public MasterRecordChange(string key, IReadOnlyList<MasterFieldChange> fields)
        {
            Key = key ?? string.Empty;
            Fields = Copy(fields);
        }

        /// <summary>正規化キーです。</summary>
        public string Key { get; }

        /// <summary>変わったフィールドです。</summary>
        public IReadOnlyList<MasterFieldChange> Fields { get; }

        static IReadOnlyList<MasterFieldChange> Copy(IReadOnlyList<MasterFieldChange> source)
        {
            if (source == null || source.Count == 0)
            {
                return new MasterFieldChange[0];
            }

            var copy = new MasterFieldChange[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }

    /// <summary>
    /// 既存レコードと新しいレコードの差分です。表示の上限はかけません。
    /// </summary>
    internal sealed class MasterDiffReport
    {
        /// <summary>差分の計算そのものが失敗したときの理由です。</summary>
        internal const string CalculationFailedReason = "差分を計算できませんでした";

        MasterDiffReport(
            MasterDiffStatus status,
            string reason,
            IReadOnlyList<string> addedKeys,
            IReadOnlyList<string> removedKeys,
            IReadOnlyList<MasterRecordChange> changes,
            int unchangedCount,
            bool orderChanged,
            bool serializedRecordsEqual)
        {
            Status = status;
            Reason = reason ?? string.Empty;
            AddedKeys = Copy(addedKeys);
            RemovedKeys = Copy(removedKeys);
            Changes = Copy(changes);
            UnchangedCount = unchangedCount < 0 ? 0 : unchangedCount;
            OrderChanged = orderChanged;
            SerializedRecordsEqual = serializedRecordsEqual;
        }

        /// <summary>計算できたかです。</summary>
        public MasterDiffStatus Status { get; }

        /// <summary>計算できない理由です。計算できたときは空です。</summary>
        public string Reason { get; }

        /// <summary>新しい側にだけあるキーです。</summary>
        public IReadOnlyList<string> AddedKeys { get; }

        /// <summary>既存側にだけあるキーです。</summary>
        public IReadOnlyList<string> RemovedKeys { get; }

        /// <summary>両方にあり、フィールドが変わったレコードです。</summary>
        public IReadOnlyList<MasterRecordChange> Changes { get; }

        /// <summary>両方にあり、フィールドが同じレコードの件数です。</summary>
        public int UnchangedCount { get; }

        /// <summary>両方にあるキーの並びだけが違うとき true です。</summary>
        public bool OrderChanged { get; }

        /// <summary>保存する records がシリアライズとして同一のとき true です。</summary>
        public bool SerializedRecordsEqual { get; }

        /// <summary>
        /// 計算できた差分を作ります。
        /// </summary>
        public static MasterDiffReport Available(
            IReadOnlyList<string> addedKeys,
            IReadOnlyList<string> removedKeys,
            IReadOnlyList<MasterRecordChange> changes,
            int unchangedCount,
            bool orderChanged,
            bool serializedRecordsEqual)
        {
            return new MasterDiffReport(
                MasterDiffStatus.Available,
                string.Empty,
                addedKeys,
                removedKeys,
                changes,
                unchangedCount,
                orderChanged,
                serializedRecordsEqual);
        }

        /// <summary>
        /// 突き合わせできない差分を作ります。件数は空です。
        /// </summary>
        /// <param name="reason">理由です。</param>
        /// <param name="serializedRecordsEqual">保存データとして同一なら true です。</param>
        /// <returns>レポートです。</returns>
        public static MasterDiffReport Unavailable(string reason, bool serializedRecordsEqual)
        {
            return new MasterDiffReport(
                MasterDiffStatus.Unavailable,
                reason,
                null,
                null,
                null,
                0,
                false,
                serializedRecordsEqual);
        }

        static IReadOnlyList<string> Copy(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return new string[0];
            }

            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i] ?? string.Empty;
            }

            return copy;
        }

        static IReadOnlyList<MasterRecordChange> Copy(IReadOnlyList<MasterRecordChange> source)
        {
            if (source == null || source.Count == 0)
            {
                return new MasterRecordChange[0];
            }

            var copy = new MasterRecordChange[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}
