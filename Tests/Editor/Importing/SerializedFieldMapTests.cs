using System;
using System.Linq;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="SerializedFieldMap"/> のテストです。
    /// </summary>
    public class SerializedFieldMapTests
    {
        [SetUp]
        public void SetUp()
        {
            SerializedFieldMap.ClearCacheForTests();
        }

        [Test]
        public void Get_TestItemRecord_IncludesIdThenNameInOrder()
        {
            var map = SerializedFieldMap.Get<TestItemRecord>();

            Assert.AreEqual(2, map.Fields.Count);
            Assert.AreEqual("id", map.Fields[0].Name);
            Assert.AreEqual("name", map.Fields[1].Name);
            Assert.AreEqual(0, map.DuplicateFieldNames.Count);
        }

        [Test]
        public void Get_TestQuestRecord_IncludesIdThenTitleInOrder()
        {
            var map = SerializedFieldMap.Get<TestQuestRecord>();

            Assert.AreEqual(2, map.Fields.Count);
            Assert.AreEqual("id", map.Fields[0].Name);
            Assert.AreEqual("title", map.Fields[1].Name);
        }

        [Test]
        public void Get_TestAllTypesRecord_ListsAllSerializedFields()
        {
            var map = SerializedFieldMap.Get<TestAllTypesRecord>();
            var names = map.Fields.Select(f => f.Name).ToArray();

            CollectionAssert.Contains(names, "id");
            CollectionAssert.Contains(names, "count");
            CollectionAssert.Contains(names, "bigCount");
            CollectionAssert.Contains(names, "ratio");
            CollectionAssert.Contains(names, "precise");
            CollectionAssert.Contains(names, "enabled");
            CollectionAssert.Contains(names, "label");
            CollectionAssert.Contains(names, "rarity");
            CollectionAssert.Contains(names, "options");
            CollectionAssert.Contains(names, "tags");
            CollectionAssert.Contains(names, "values");
            CollectionAssert.Contains(names, "nested");
        }

        [Test]
        public void Get_CachesSameInstanceForSameType()
        {
            var first = SerializedFieldMap.Get<TestItemRecord>();
            var second = SerializedFieldMap.Get<TestItemRecord>();

            Assert.AreSame(first, second);
        }

        [Test]
        public void Get_ExcludesNonSerializedStaticConstAndReadonlyFields()
        {
            var map = SerializedFieldMap.Get<FieldFilterTestRecord>();
            var names = map.Fields.Select(f => f.Name).ToArray();

            CollectionAssert.Contains(names, "included");
            CollectionAssert.DoesNotContain(names, "excludedNonSerialized");
            CollectionAssert.DoesNotContain(names, "excludedStatic");
            CollectionAssert.DoesNotContain(names, "excludedConst");
            CollectionAssert.DoesNotContain(names, "excludedReadonly");
            CollectionAssert.DoesNotContain(names, "k_BackingField");
            Assert.AreEqual(1, new FieldFilterTestRecord().ExcludedReadonlyValue);
        }

        [Test]
        public void Get_DuplicateIdFieldName_IsReportedInDuplicateFieldNames()
        {
            var map = SerializedFieldMap.Get<DuplicateIdFieldTestRecord>();

            CollectionAssert.Contains(map.DuplicateFieldNames, "id");
        }

        [Test]
        public void Import_DuplicateFieldName_ReturnsErrorAndSucceededFalse()
        {
            const string json = "{ \"records\": [ { \"id\": 1 } ] }";
            var result = MasterJsonImporter.Import<DuplicateIdFieldTestRecord>(json);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(0, result.Records.Count);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == ImportIssueKind.DuplicateFieldName));
        }

        [Serializable]
        sealed class FieldFilterTestRecord : IntIdMasterRecord
        {
            [SerializeField]
            int included;

            [NonSerialized]
            int excludedNonSerialized;

            static int excludedStatic;

            const int excludedConst = 1;

            readonly int excludedReadonly = 1;

            internal int ExcludedReadonlyValue => excludedReadonly;

            int k_BackingField;

            public int Included => included;
        }

        [Serializable]
        sealed class DuplicateIdFieldTestRecord : IntIdMasterRecord
        {
            [SerializeField]
            int id;
        }
    }
}
