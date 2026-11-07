using System;
using System.Linq;
using MasterDataTool.EditorTools.Tests.Golden;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterTypeCatalog"/> のテストです。
    /// </summary>
    public sealed class MasterTypeCatalogTests
    {
        [Test]
        public void IncludeAll_ContainsConcreteTestMasters()
        {
            var catalog = new MasterTypeCatalog(_ => true);

            Assert.IsTrue(Contains(catalog, typeof(TestItemMaster)));
            Assert.IsTrue(Contains(catalog, typeof(TestQuestMaster)));
            Assert.IsTrue(Contains(catalog, typeof(TestRefMaster)));
            Assert.IsTrue(Contains(catalog, typeof(TestNodeMaster)));
            Assert.IsTrue(Contains(catalog, typeof(TestRequiredMaster)));
            Assert.IsTrue(Contains(catalog, typeof(GoldenItemMaster)));
            Assert.IsTrue(Contains(catalog, typeof(GoldenQuestMaster)));
            Assert.IsFalse(Contains(catalog, typeof(TestAbstractMaster)));
            Assert.IsFalse(catalog.Types.Any(info => info.MasterType.IsGenericTypeDefinition));
        }

        [Test]
        public void IncludeAll_SetsRecordTypeIdKindAndDisplayName()
        {
            var catalog = new MasterTypeCatalog(_ => true);
            var item = Find(catalog, typeof(TestItemMaster));
            var quest = Find(catalog, typeof(TestQuestMaster));

            Assert.AreEqual(typeof(TestItemRecord), item.RecordType);
            Assert.AreEqual(MasterIdKind.Int, item.IdKind);
            Assert.AreEqual(typeof(TestItemMaster).FullName, item.DisplayName);
            Assert.AreEqual(typeof(TestQuestRecord), quest.RecordType);
            Assert.AreEqual(MasterIdKind.String, quest.IdKind);
            Assert.AreEqual(typeof(TestQuestMaster).FullName, quest.DisplayName);
        }

        [Test]
        public void IncludeAll_SortsByDisplayNameOrdinal()
        {
            var catalog = new MasterTypeCatalog(_ => true);
            var types = catalog.Types;
            for (var i = 1; i < types.Count; i++)
            {
                Assert.LessOrEqual(
                    string.CompareOrdinal(types[i - 1].DisplayName, types[i].DisplayName),
                    0);
            }
        }

        [Test]
        public void DefaultFilter_ExcludesTestAssemblyTypes()
        {
            var catalog = new MasterTypeCatalog();

            Assert.IsFalse(Contains(catalog, typeof(TestItemMaster)));
            Assert.IsFalse(Contains(catalog, typeof(TestQuestMaster)));
            Assert.IsFalse(Contains(catalog, typeof(GoldenItemMaster)));
            for (var i = 0; i < catalog.Types.Count; i++)
            {
                var assemblyName = catalog.Types[i].MasterType.Assembly.GetName().Name;
                Assert.IsFalse(assemblyName != null && assemblyName.EndsWith(".Tests", StringComparison.Ordinal));
            }
        }

        [Test]
        public void CustomFilter_IncludesOnlyMatchingType()
        {
            var catalog = new MasterTypeCatalog(type => type == typeof(TestItemMaster));

            Assert.AreEqual(1, catalog.Types.Count);
            Assert.AreEqual(typeof(TestItemMaster), catalog.Types[0].MasterType);
        }

        [Test]
        public void TryFind_FindsExactDisplayName()
        {
            var catalog = new MasterTypeCatalog(_ => true);
            var fullName = typeof(TestItemMaster).FullName;

            Assert.IsTrue(catalog.TryFind(fullName, out var info));
            Assert.AreEqual(typeof(TestItemMaster), info.MasterType);
        }

        [Test]
        public void TryFind_RejectsMissingNullAndEmpty()
        {
            var catalog = new MasterTypeCatalog(_ => true);

            Assert.IsFalse(catalog.TryFind(null, out _));
            Assert.IsFalse(catalog.TryFind(string.Empty, out _));
            Assert.IsFalse(catalog.TryFind("Missing.Type", out _));
        }

        [Test]
        public void TryFind_IsCaseSensitive()
        {
            var catalog = new MasterTypeCatalog(_ => true);
            var fullName = typeof(TestItemMaster).FullName;
            var lower = fullName.ToLowerInvariant();

            Assert.AreNotEqual(fullName, lower);
            Assert.IsFalse(catalog.TryFind(lower, out _));
        }

        [Test]
        public void Refresh_RebuildsSameContent()
        {
            var catalog = new MasterTypeCatalog(_ => true);
            var before = catalog.Types.Select(info => info.DisplayName).ToArray();

            Assert.DoesNotThrow(() => catalog.Refresh());

            var after = catalog.Types.Select(info => info.DisplayName).ToArray();
            CollectionAssert.AreEqual(before, after);
        }

        static bool Contains(MasterTypeCatalog catalog, Type masterType)
        {
            return catalog.Types.Any(info => info.MasterType == masterType);
        }

        static MasterTypeInfo Find(MasterTypeCatalog catalog, Type masterType)
        {
            return catalog.Types.First(info => info.MasterType == masterType);
        }
    }
}
