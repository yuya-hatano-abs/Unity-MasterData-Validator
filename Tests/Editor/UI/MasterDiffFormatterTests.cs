using System.Collections.Generic;
using MasterDataTool.EditorTools.Diff;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterDiffFormatter"/> のテストです。
    /// </summary>
    public sealed class MasterDiffFormatterTests
    {
        [Test]
        public void FormatSummary_Unavailable_IncludesReason()
        {
            var report = MasterDiffReport.Unavailable("新規: 重複キー 1", false);
            Assert.AreEqual("差分を計算できません。新規: 重複キー 1", MasterDiffFormatter.FormatSummary(report));
            CollectionAssert.AreEqual(new[] { "新規: 重複キー 1" }, MasterDiffFormatter.FormatDetails(report));
        }

        [Test]
        public void FormatSummary_Counts()
        {
            var report = MasterDiffReport.Available(
                new[] { "2", "3" },
                new[] { "9" },
                new[] { Change("1", Field("name", "A", "B")) },
                10,
                false,
                false);

            Assert.AreEqual("追加 2 / 削除 1 / 変更 1 / 変更なし 10", MasterDiffFormatter.FormatSummary(report));
        }

        [Test]
        public void FormatSummary_OrderChangedSuffix()
        {
            var report = MasterDiffReport.Available(
                new[] { "3" },
                new string[0],
                new MasterRecordChange[0],
                2,
                true,
                false);

            Assert.AreEqual("追加 1 / 削除 0 / 変更 0 / 変更なし 2 (並び順の変更あり)", MasterDiffFormatter.FormatSummary(report));
        }

        [Test]
        public void FormatSummary_OrderOnly()
        {
            var report = MasterDiffReport.Available(new string[0], new string[0], new MasterRecordChange[0], 2, true, false);
            Assert.AreEqual("ID・フィールドの変更なし(並び順のみ変更)", MasterDiffFormatter.FormatSummary(report));
        }

        [Test]
        public void FormatSummary_NoFieldDiffButSerializedDiffers()
        {
            var report = MasterDiffReport.Available(new string[0], new string[0], new MasterRecordChange[0], 2, false, false);
            Assert.AreEqual(
                "ID・フィールドの差分は検出されませんでした(保存データには差異があります)",
                MasterDiffFormatter.FormatSummary(report));
        }

        [Test]
        public void FormatSummary_SerializedEqual_IsUnchanged()
        {
            var report = MasterDiffReport.Available(new string[0], new string[0], new MasterRecordChange[0], 2, false, true);
            Assert.AreEqual("変更なし", MasterDiffFormatter.FormatSummary(report));
        }

        [Test]
        public void FormatDetails_UsesPlusMinusTilde()
        {
            var report = MasterDiffReport.Available(
                new[] { "2" },
                new[] { "9" },
                new[] { Change("1", Field("name", "Potion", "Ether")) },
                0,
                false,
                false);

            CollectionAssert.AreEqual(
                new[]
                {
                    "+ 2",
                    "- 9",
                    "~ 1",
                    "  name: Potion → Ether",
                },
                MasterDiffFormatter.FormatDetails(report));
        }

        [Test]
        public void FormatDetails_LimitsKeysAt20()
        {
            var twenty = Keys(20);
            var twentyOne = Keys(21);
            var exact = MasterDiffFormatter.FormatDetails(
                MasterDiffReport.Available(twenty, new string[0], new MasterRecordChange[0], 0, false, false));
            var over = MasterDiffFormatter.FormatDetails(
                MasterDiffReport.Available(twentyOne, new string[0], new MasterRecordChange[0], 0, false, false));

            Assert.AreEqual(20, exact.Count);
            Assert.AreEqual("+ k20", exact[19]);
            Assert.AreEqual(21, over.Count);
            Assert.AreEqual("+ k20", over[19]);
            Assert.AreEqual("…ほか 1 件", over[20]);
        }

        [Test]
        public void FormatDetails_LimitsFieldsAt5()
        {
            var exact = MasterDiffFormatter.FormatDetails(Changed(5));
            var over = MasterDiffFormatter.FormatDetails(Changed(6));

            Assert.AreEqual(6, exact.Count);
            Assert.AreEqual("~ 1", exact[0]);
            Assert.AreEqual("  f5: a → b", exact[5]);
            Assert.AreEqual(7, over.Count);
            Assert.AreEqual("  f5: a → b", over[5]);
            Assert.AreEqual("  …ほか 1 フィールド", over[6]);
        }

        [Test]
        public void FormatDetails_TruncatesValueAt40()
        {
            var forty = new string('a', MasterDiffFormatter.MaxValueCharacters);
            var fortyOne = new string('b', MasterDiffFormatter.MaxValueCharacters + 1);
            var lines = MasterDiffFormatter.FormatDetails(
                MasterDiffReport.Available(
                    new string[0],
                    new string[0],
                    new[] { Change("1", Field("label", forty, fortyOne)) },
                    0,
                    false,
                    false));

            Assert.AreEqual("  label: " + forty + " → " + new string('b', 40) + "…", lines[1]);
        }

        [Test]
        public void FormatDetails_LimitsCollectionElementsAt5()
        {
            var five = Elements(5);
            var six = Elements(6);
            var exact = MasterDiffFormatter.FormatDetails(CollectionChange(five));
            var over = MasterDiffFormatter.FormatDetails(CollectionChange(six));

            Assert.AreEqual("  tags: [e1, e2, e3, e4, e5] → [e1, e2, e3, e4, e5]", exact[1]);
            Assert.AreEqual(
                "  tags: [e1, e2, e3, e4, e5] → [e1, e2, e3, e4, e5…(+1)]",
                over[1]);
        }

        [Test]
        public void FormatSummary_Null_Throws()
        {
            var exception = Assert.Throws<System.ArgumentNullException>(() => MasterDiffFormatter.FormatSummary(null));
            Assert.AreEqual("report", exception.ParamName);
        }

        static MasterDiffReport Changed(int fieldCount)
        {
            var fields = new List<MasterFieldChange>();
            for (var i = 0; i < fieldCount; i++)
            {
                fields.Add(Field("f" + (i + 1), "a", "b"));
            }

            return MasterDiffReport.Available(
                new string[0],
                new string[0],
                new[] { new MasterRecordChange("1", fields) },
                0,
                false,
                false);
        }

        static MasterDiffReport CollectionChange(IReadOnlyList<string> incoming)
        {
            var field = new MasterFieldChange(
                "tags",
                MasterDiffValue.Collection(Elements(5)),
                MasterDiffValue.Collection(incoming));
            return MasterDiffReport.Available(
                new string[0],
                new string[0],
                new[] { new MasterRecordChange("1", new[] { field }) },
                0,
                false,
                false);
        }

        static string[] Keys(int count)
        {
            var keys = new string[count];
            for (var i = 0; i < count; i++)
            {
                keys[i] = "k" + (i + 1);
            }

            return keys;
        }

        static string[] Elements(int count)
        {
            var elements = new string[count];
            for (var i = 0; i < count; i++)
            {
                elements[i] = "e" + (i + 1);
            }

            return elements;
        }

        static MasterRecordChange Change(string key, MasterFieldChange field)
        {
            return new MasterRecordChange(key, new[] { field });
        }

        static MasterFieldChange Field(string name, string oldText, string newText)
        {
            return new MasterFieldChange(name, MasterDiffValue.Scalar(oldText), MasterDiffValue.Scalar(newText));
        }
    }
}
