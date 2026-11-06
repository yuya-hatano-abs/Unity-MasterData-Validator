using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationContext"/> のテストです。
    /// </summary>
    public class ValidationContextTests
    {
        static MasterTable CreateTable(string name, Type masterType, MasterIdKind idKind)
        {
            return new MasterTable(
                name,
                masterType,
                idKind == MasterIdKind.Int ? typeof(TestItemRecord) : typeof(TestQuestRecord),
                idKind,
                Array.Empty<IMasterRecord>(),
                null);
        }

        [Test]
        public void Constructor_OmittedTargets_EqualsTables()
        {
            var item = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            var context = new ValidationContext(new[] { item });

            Assert.AreEqual(1, context.Tables.Count);
            Assert.AreEqual(1, context.Targets.Count);
            Assert.AreSame(item, context.Targets[0]);
        }

        [Test]
        public void Constructor_WithTargets_SubsetOfTables()
        {
            var item = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            var quest = CreateTable("TestQuestMaster", typeof(TestQuestMaster), MasterIdKind.String);
            var context = new ValidationContext(new[] { item, quest }, new[] { quest });

            Assert.AreEqual(2, context.Tables.Count);
            Assert.AreEqual(1, context.Targets.Count);
            Assert.AreSame(quest, context.Targets[0]);
        }

        [Test]
        public void Constructor_TargetNotSameReference_ThrowsArgumentException()
        {
            var item = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            var duplicate = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            Assert.Throws<ArgumentException>(() => new ValidationContext(new[] { item }, new[] { duplicate }));
        }

        [Test]
        public void Constructor_DuplicateName_ThrowsArgumentException()
        {
            var a = CreateTable("Same", typeof(TestItemMaster), MasterIdKind.Int);
            var b = new MasterTable(
                "Same",
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                MasterIdKind.String,
                Array.Empty<IMasterRecord>(),
                null);
            Assert.Throws<ArgumentException>(() => new ValidationContext(new[] { a, b }));
        }

        [Test]
        public void Constructor_DuplicateMasterType_ThrowsArgumentException()
        {
            var a = CreateTable("A", typeof(TestItemMaster), MasterIdKind.Int);
            var b = new MasterTable(
                "B",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                Array.Empty<IMasterRecord>(),
                null);
            Assert.Throws<ArgumentException>(() => new ValidationContext(new[] { a, b }));
        }

        [Test]
        public void Constructor_AllowsMultipleNullMasterTypesWithDistinctNames()
        {
            var a = new MasterTable("A", null, typeof(TestItemRecord), MasterIdKind.Int, Array.Empty<IMasterRecord>(), null);
            var b = new MasterTable("B", null, typeof(TestQuestRecord), MasterIdKind.String, Array.Empty<IMasterRecord>(), null);
            var context = new ValidationContext(new[] { a, b });
            Assert.AreEqual(2, context.Tables.Count);
        }

        [Test]
        public void Constructor_EmptyTables_Succeeds()
        {
            var context = new ValidationContext(Array.Empty<MasterTable>());
            Assert.AreEqual(0, context.Tables.Count);
            Assert.AreEqual(0, context.Targets.Count);
        }

        [Test]
        public void TryGetTable_FindsByMasterType()
        {
            var item = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            var context = new ValidationContext(new[] { item });

            Assert.IsTrue(context.TryGetTable(typeof(TestItemMaster), out var found));
            Assert.AreSame(item, found);
            Assert.IsFalse(context.TryGetTable(typeof(TestQuestMaster), out _));
            Assert.IsFalse(context.TryGetTable(null, out _));
        }

        [Test]
        public void TryGetTableByName_IsCaseSensitive()
        {
            var item = CreateTable("TestItemMaster", typeof(TestItemMaster), MasterIdKind.Int);
            var context = new ValidationContext(new[] { item });

            Assert.IsTrue(context.TryGetTableByName("TestItemMaster", out var found));
            Assert.AreSame(item, found);
            Assert.IsFalse(context.TryGetTableByName("testitemmaster", out _));
            Assert.IsFalse(context.TryGetTableByName(null, out _));
        }

        [Test]
        public void Constructor_NullTables_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationContext(null));
        }

        [Test]
        public void Constructor_NullTableElement_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ValidationContext(new MasterTable[] { null }));
        }
    }
}
