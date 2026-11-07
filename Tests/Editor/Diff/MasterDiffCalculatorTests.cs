using System.Collections.Generic;
using System.Reflection;
using MasterDataTool.EditorTools.Diff;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterDiffCalculator"/> のテストです。アセットファイルは保存しません。
    /// </summary>
    public sealed class MasterDiffCalculatorTests
    {
        [Test]
        public void Calculate_AddedKey()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "Potion")),
                Records(new TestItemRecord(1, "Potion"), new TestItemRecord(2, "Ether")));

            Assert.AreEqual(MasterDiffStatus.Available, report.Status);
            CollectionAssert.AreEqual(new[] { "2" }, report.AddedKeys);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.Changes.Count);
            Assert.AreEqual(1, report.UnchangedCount);
            Assert.IsFalse(report.OrderChanged);
        }

        [Test]
        public void Calculate_RemovedKey()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "Potion"), new TestItemRecord(2, "Ether")),
                Records(new TestItemRecord(1, "Potion")));

            CollectionAssert.AreEqual(new[] { "2" }, report.RemovedKeys);
            Assert.AreEqual(0, report.AddedKeys.Count);
            Assert.AreEqual(1, report.UnchangedCount);
            Assert.IsFalse(report.OrderChanged);
        }

        [Test]
        public void Calculate_ChangedField()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "Potion")),
                Records(new TestItemRecord(1, "Ether")));

            Assert.AreEqual(0, report.AddedKeys.Count);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.UnchangedCount);
            Assert.AreEqual(1, report.Changes.Count);
            Assert.AreEqual("1", report.Changes[0].Key);
            var field = FindField(report.Changes[0], "name");
            Assert.AreEqual("Potion", field.OldValue.Text);
            Assert.AreEqual("Ether", field.NewValue.Text);
            Assert.IsFalse(report.OrderChanged);
        }

        [Test]
        public void Calculate_Unchanged()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "Potion"), new TestItemRecord(2, "Ether")),
                Records(new TestItemRecord(1, "Potion"), new TestItemRecord(2, "Ether")));

            Assert.AreEqual(0, report.AddedKeys.Count);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.Changes.Count);
            Assert.AreEqual(2, report.UnchangedCount);
            Assert.IsFalse(report.OrderChanged);
        }

        [Test]
        public void Calculate_OrderOnly()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "A"), new TestItemRecord(2, "B")),
                Records(new TestItemRecord(2, "B"), new TestItemRecord(1, "A")));

            Assert.AreEqual(0, report.AddedKeys.Count);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.Changes.Count);
            Assert.AreEqual(2, report.UnchangedCount);
            Assert.IsTrue(report.OrderChanged);
        }

        [Test]
        public void Calculate_AddedAndOrderChanged()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "A"), new TestItemRecord(2, "B")),
                Records(new TestItemRecord(2, "B"), new TestItemRecord(1, "A"), new TestItemRecord(3, "C")));

            CollectionAssert.AreEqual(new[] { "3" }, report.AddedKeys);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.Changes.Count);
            Assert.IsTrue(report.OrderChanged);
        }

        [Test]
        public void Calculate_IncomingDuplicateKey_IsUnavailable()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "A")),
                Records(new TestItemRecord(1, "A"), new TestItemRecord(1, "B")));

            AssertUnavailable(report, "新規", "重複キー 1");
        }

        [Test]
        public void Calculate_ExistingDuplicateKey_IsUnavailable()
        {
            var report = Calculate(
                Records(new TestItemRecord(1, "A"), new TestItemRecord(1, "B")),
                Records(new TestItemRecord(1, "A")));

            AssertUnavailable(report, "既存", "重複キー 1");
        }

        [Test]
        public void Calculate_EmptyStringKey_IsUnavailable()
        {
            var report = Calculate(
                Records(new TestQuestRecord("Quest_A", "First")),
                Records(new TestQuestRecord(string.Empty, "First")));

            AssertUnavailable(report, "新規", "空のキー 1 件");
        }

        [Test]
        public void Calculate_NullRecord_IsUnavailable()
        {
            var report = Calculate(
                Records(null, new TestItemRecord(1, "A")),
                Records(new TestItemRecord(1, "A")));

            AssertUnavailable(report, "既存", "null 1 件");
        }

        [Test]
        public void Calculate_StringId_IsOrdinal()
        {
            var report = Calculate(
                Records(new TestQuestRecord("Ab", "First")),
                Records(new TestQuestRecord("ab", "First")));

            Assert.AreEqual(MasterDiffStatus.Available, report.Status);
            CollectionAssert.AreEqual(new[] { "Ab" }, report.RemovedKeys);
            CollectionAssert.AreEqual(new[] { "ab" }, report.AddedKeys);
            Assert.AreEqual(0, report.Changes.Count);
        }

        [Test]
        public void Calculate_FloatChange()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(previous, "ratio", 1f);
            SetField(incoming, "ratio", 2f);

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            var field = FindField(report.Changes[0], "ratio");
            Assert.AreEqual("1", field.OldValue.Text);
            Assert.AreEqual("2", field.NewValue.Text);
        }

        [Test]
        public void Calculate_FloatNaN_IsEqual()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(previous, "ratio", float.NaN);
            SetField(incoming, "ratio", float.NaN);

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            Assert.AreEqual(0, report.Changes.Count);
            Assert.AreEqual(1, report.UnchangedCount);
        }

        [Test]
        public void Calculate_ListElementChange()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(previous, "tags", new List<string> { "a", "b" });
            SetField(incoming, "tags", new List<string> { "a", "c" });

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            var field = FindField(report.Changes[0], "tags");
            Assert.IsTrue(field.OldValue.IsCollection);
            CollectionAssert.AreEqual(new[] { "a", "b" }, field.OldValue.Elements);
            CollectionAssert.AreEqual(new[] { "a", "c" }, field.NewValue.Elements);
        }

        [Test]
        public void Calculate_ListCountChange()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(previous, "tags", new List<string> { "a" });
            SetField(incoming, "tags", new List<string> { "a", "b" });

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            var field = FindField(report.Changes[0], "tags");
            Assert.AreEqual(1, field.OldValue.Elements.Count);
            Assert.AreEqual(2, field.NewValue.Elements.Count);
        }

        [Test]
        public void Calculate_NestedChange()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(previous, "nested", new TestNestedValue { value = 1 });
            SetField(incoming, "nested", new TestNestedValue { value = 2 });

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            var field = FindField(report.Changes[0], "nested");
            StringAssert.Contains("1", field.OldValue.Text);
            StringAssert.Contains("2", field.NewValue.Text);
            Assert.AreNotEqual(field.OldValue.Text, field.NewValue.Text);
        }

        [Test]
        public void Calculate_NullAndEmptyString_AreDifferent()
        {
            var previous = new TestAllTypesRecord(1);
            var incoming = new TestAllTypesRecord(1);
            SetField(incoming, "label", string.Empty);

            var report = Calculate(Records(previous), Records(incoming), typeof(TestAllTypesRecord));
            var field = FindField(report.Changes[0], "label");
            Assert.AreEqual("(null)", field.OldValue.Text);
            Assert.AreEqual("(空)", field.NewValue.Text);
        }

        [Test]
        public void Calculate_SerializedEqualRecords_HaveNoFieldOrOrderDiff()
        {
            var stored = new[]
            {
                new TestItemRecord(1, "Potion"),
                new TestItemRecord(2, "Ether"),
            };
            var incoming = new[]
            {
                new TestItemRecord(1, "Potion"),
                new TestItemRecord(2, "Ether"),
            };
            var asset = ScriptableObject.CreateInstance<TestItemMaster>();
            try
            {
                asset.ReplaceRecords(stored);
                var equal = MasterAssetSaver.SerializedRecordsEqual(asset, incoming);
                Assert.IsTrue(equal);

                var report = MasterDiffCalculator.Calculate(
                    asset.AllRecords,
                    Records(incoming[0], incoming[1]),
                    typeof(TestItemRecord),
                    equal);

                Assert.AreEqual(MasterDiffStatus.Available, report.Status);
                Assert.AreEqual(0, report.AddedKeys.Count);
                Assert.AreEqual(0, report.RemovedKeys.Count);
                Assert.AreEqual(0, report.Changes.Count);
                Assert.AreEqual(2, report.UnchangedCount);
                Assert.IsFalse(report.OrderChanged);
                Assert.IsTrue(report.SerializedRecordsEqual);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        static MasterDiffReport Calculate(IReadOnlyList<IMasterRecord> existing, IReadOnlyList<IMasterRecord> incoming)
        {
            var recordType = FirstType(existing);
            if (recordType == null)
            {
                recordType = FirstType(incoming);
            }

            return MasterDiffCalculator.Calculate(existing, incoming, recordType, false);
        }

        static MasterDiffReport Calculate(
            IReadOnlyList<IMasterRecord> existing,
            IReadOnlyList<IMasterRecord> incoming,
            System.Type recordType)
        {
            return MasterDiffCalculator.Calculate(existing, incoming, recordType, false);
        }

        static System.Type FirstType(IReadOnlyList<IMasterRecord> records)
        {
            for (var i = 0; i < records.Count; i++)
            {
                if (records[i] != null)
                {
                    return records[i].GetType();
                }
            }

            return null;
        }

        static void AssertUnavailable(MasterDiffReport report, string side, string detail)
        {
            Assert.AreEqual(MasterDiffStatus.Unavailable, report.Status);
            StringAssert.Contains(side, report.Reason);
            StringAssert.Contains(detail, report.Reason);
            Assert.AreEqual(0, report.AddedKeys.Count);
            Assert.AreEqual(0, report.RemovedKeys.Count);
            Assert.AreEqual(0, report.Changes.Count);
            Assert.AreEqual(0, report.UnchangedCount);
            Assert.IsFalse(report.OrderChanged);
        }

        static IReadOnlyList<IMasterRecord> Records(params IMasterRecord[] records)
        {
            return records;
        }

        static MasterFieldChange FindField(MasterRecordChange change, string name)
        {
            for (var i = 0; i < change.Fields.Count; i++)
            {
                if (change.Fields[i].Name == name)
                {
                    return change.Fields[i];
                }
            }

            Assert.Fail("フィールドがありません: " + name);
            return null;
        }

        static void SetField(object target, string name, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail("フィールドがありません: " + name);
        }
    }
}
