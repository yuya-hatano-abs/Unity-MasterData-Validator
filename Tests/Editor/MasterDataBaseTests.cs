using System;
using System.Collections.Generic;
using System.Globalization;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterDataBase{TRecord}"/> の振る舞いを検証します。
    /// </summary>
    public class MasterDataBaseTests
    {
        TestItemMaster itemMaster;
        TestQuestMaster questMaster;

        [SetUp]
        public void SetUp()
        {
            itemMaster = ScriptableObject.CreateInstance<TestItemMaster>();
            questMaster = ScriptableObject.CreateInstance<TestQuestMaster>();
        }

        [TearDown]
        public void TearDown()
        {
            if (itemMaster != null)
            {
                UnityEngine.Object.DestroyImmediate(itemMaster);
            }

            if (questMaster != null)
            {
                UnityEngine.Object.DestroyImmediate(questMaster);
            }
        }

        [Test]
        public void IdKind_EmptyRecords_ItemMasterIsInt()
        {
            Assert.AreEqual(MasterIdKind.Int, itemMaster.IdKind);
        }

        [Test]
        public void IdKind_EmptyRecords_QuestMasterIsString()
        {
            Assert.AreEqual(MasterIdKind.String, questMaster.IdKind);
        }

        [Test]
        public void RecordKey_IntId_UsesInvariantCultureString()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(42, "foo") });

            Assert.IsTrue(itemMaster.TryGetByKey("42", out var record));
            Assert.AreEqual(42, record.Id);
        }

        [Test]
        public void RecordKey_StringId_MatchesIdValue()
        {
            questMaster.ReplaceRecords(new[] { new TestQuestRecord("Quest_A", "Title") });

            Assert.IsTrue(questMaster.TryGetByKey("Quest_A", out var record));
            Assert.AreEqual("Quest_A", record.Id);
        }

        [Test]
        public void ReplaceRecords_SetsCountAndExposesSameRecordsViaAllRecords()
        {
            var rows = new[]
            {
                new TestItemRecord(1, "a"),
                new TestItemRecord(2, "b"),
                new TestItemRecord(3, "c")
            };
            itemMaster.ReplaceRecords(rows);

            Assert.AreEqual(3, itemMaster.Count);
            Assert.AreEqual(3, itemMaster.Records.Count);
            Assert.AreEqual(3, itemMaster.AllRecords.Count);
            Assert.AreEqual("a", itemMaster.Records[0].Name);
            Assert.AreEqual("a", ((TestItemRecord)itemMaster.AllRecords[0]).Name);
        }

        [Test]
        public void RecordType_ReturnsConcreteRecordType()
        {
            Assert.AreEqual(typeof(TestItemRecord), itemMaster.RecordType);
            Assert.AreEqual(typeof(TestQuestRecord), questMaster.RecordType);
        }

        [Test]
        public void TryGetByKey_ReturnsTrueWhenKeyExists()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(10, "ten") });

            Assert.IsTrue(itemMaster.TryGetByKey("10", out var byKey));
            Assert.IsTrue(itemMaster.TryGetRecordByKey("10", out IMasterRecord byInterface));
            Assert.AreSame(byKey, byInterface);
            Assert.AreEqual("ten", byKey.Name);
        }

        [Test]
        public void TryGetByKey_ReturnsFalseWhenKeyMissing()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(10, "ten") });

            Assert.IsFalse(itemMaster.TryGetByKey("999", out _));
            Assert.IsFalse(itemMaster.TryGetRecordByKey("999", out _));
        }

        [Test]
        public void TryGetById_IntTable_FindsByIntId()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(7, "seven") });

            Assert.IsTrue(itemMaster.TryGetById(7, out var record));
            Assert.AreEqual("seven", record.Name);
        }

        [Test]
        public void TryGetById_IntTable_ReturnsFalseForMissingId()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(7, "seven") });

            Assert.IsFalse(itemMaster.TryGetById(8, out _));
        }

        [Test]
        public void TryGetById_StringTable_FindsByStringId()
        {
            questMaster.ReplaceRecords(new[] { new TestQuestRecord("q1", "First") });

            Assert.IsTrue(questMaster.TryGetById("q1", out var record));
            Assert.AreEqual("First", record.Title);
        }

        [Test]
        public void TryGetById_StringTable_ReturnsFalseForMissingId()
        {
            questMaster.ReplaceRecords(new[] { new TestQuestRecord("q1", "First") });

            Assert.IsFalse(questMaster.TryGetById("missing", out _));
        }

        [Test]
        public void TryGetById_WrongOverloadOnStringTable_ReturnsFalseWithoutThrowing()
        {
            questMaster.ReplaceRecords(new[] { new TestQuestRecord("q1", "First") });

            Assert.IsFalse(questMaster.TryGetById(1, out TestQuestRecord _));
        }

        [Test]
        public void TryGetById_WrongOverloadOnIntTable_ReturnsFalseWithoutThrowing()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(1, "one") });

            Assert.IsFalse(itemMaster.TryGetById("1", out TestItemRecord _));
        }

        [Test]
        public void TryGetByKey_StringId_IsCaseSensitive()
        {
            questMaster.ReplaceRecords(new[]
            {
                new TestQuestRecord("Quest_A", "Upper"),
                new TestQuestRecord("quest_a", "Lower")
            });

            Assert.IsTrue(questMaster.TryGetByKey("Quest_A", out var upper));
            Assert.IsTrue(questMaster.TryGetByKey("quest_a", out var lower));
            Assert.AreEqual("Upper", upper.Title);
            Assert.AreEqual("Lower", lower.Title);
        }

        [Test]
        public void TryGetByKey_DuplicateKeys_ReturnsFirstRecord()
        {
            var first = new TestItemRecord(5, "first");
            var duplicate = new TestItemRecord(5, "second");
            itemMaster.ReplaceRecords(new[] { first, duplicate });

            Assert.IsTrue(itemMaster.TryGetByKey("5", out var found));
            Assert.AreEqual("first", found.Name);
        }

        [Test]
        public void TryGetByKey_IgnoresNullRecordsAndEmptyStringIds()
        {
            questMaster.ReplaceRecords(new List<TestQuestRecord>
            {
                null,
                new TestQuestRecord(null, "empty-id"),
                new TestQuestRecord(string.Empty, "blank-id"),
                new TestQuestRecord("valid", "ok")
            });

            Assert.IsFalse(questMaster.TryGetByKey(string.Empty, out _));
            Assert.IsTrue(questMaster.TryGetByKey("valid", out var record));
            Assert.AreEqual("ok", record.Title);
        }

        [Test]
        public void ReplaceRecords_RefreshesLookupCache()
        {
            itemMaster.ReplaceRecords(new[] { new TestItemRecord(1, "old") });
            Assert.IsTrue(itemMaster.TryGetById(1, out _));

            itemMaster.ReplaceRecords(new[] { new TestItemRecord(2, "new") });

            Assert.IsFalse(itemMaster.TryGetById(1, out _));
            Assert.IsTrue(itemMaster.TryGetById(2, out var record));
            Assert.AreEqual("new", record.Name);
        }

        [Test]
        public void ReplaceRecords_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => itemMaster.ReplaceRecords(null));
        }

        [Test]
        public void JsonUtility_RoundTrip_PreservesItemMasterRecords()
        {
            itemMaster.ReplaceRecords(new[]
            {
                new TestItemRecord(100, "Cent"),
                new TestItemRecord(200, "TwoHundred")
            });

            var json = JsonUtility.ToJson(itemMaster);
            var restored = ScriptableObject.CreateInstance<TestItemMaster>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, restored);

                Assert.AreEqual(2, restored.Count);
                Assert.IsTrue(restored.TryGetById(100, out var r100));
                Assert.IsTrue(restored.TryGetById(200, out var r200));
                Assert.AreEqual("Cent", r100.Name);
                Assert.AreEqual("TwoHundred", r200.Name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        [Test]
        public void JsonUtility_RoundTrip_PreservesQuestMasterRecords()
        {
            questMaster.ReplaceRecords(new[]
            {
                new TestQuestRecord("Q_A", "Alpha"),
                new TestQuestRecord("Q_B", "Beta")
            });

            var json = JsonUtility.ToJson(questMaster);
            var restored = ScriptableObject.CreateInstance<TestQuestMaster>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, restored);

                Assert.AreEqual(2, restored.Count);
                Assert.IsTrue(restored.TryGetById("Q_A", out var qa));
                Assert.IsTrue(restored.TryGetById("Q_B", out var qb));
                Assert.AreEqual("Alpha", qa.Title);
                Assert.AreEqual("Beta", qb.Title);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        [Test]
        public void IntRecordKey_UsesInvariantCulture()
        {
            var record = new TestItemRecord(1000, "n");
            Assert.AreEqual(1000.ToString(CultureInfo.InvariantCulture), record.Key);
        }
    }
}
