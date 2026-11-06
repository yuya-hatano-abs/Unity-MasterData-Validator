using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterRefAttribute"/> のテストです。
    /// </summary>
    public class MasterRefAttributeTests
    {
        [Test]
        public void Constructor_StoresTargetMasterType()
        {
            var attribute = new MasterRefAttribute(typeof(TestItemMaster));
            Assert.AreEqual(typeof(TestItemMaster), attribute.TargetMasterType);
        }

        [Test]
        public void NoneValue_NotSpecified_HasNoneValueIsFalse()
        {
            var attribute = new MasterRefAttribute(typeof(TestItemMaster));
            Assert.IsFalse(attribute.HasNoneValue);
        }

        [Test]
        public void NoneValue_Zero_HasNoneValueIsTrue()
        {
            var attribute = new MasterRefAttribute(typeof(TestItemMaster)) { NoneValue = 0 };
            Assert.IsTrue(attribute.HasNoneValue);
            Assert.AreEqual(0, attribute.NoneValue);
        }

        [Test]
        public void NoneValue_Five_StoresFive()
        {
            var attribute = new MasterRefAttribute(typeof(TestItemMaster)) { NoneValue = 5 };
            Assert.IsTrue(attribute.HasNoneValue);
            Assert.AreEqual(5, attribute.NoneValue);
        }

        [Test]
        public void AttributeUsage_FieldOnlyAllowMultipleFalse()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(MasterRefAttribute),
                typeof(AttributeUsageAttribute));
            Assert.NotNull(usage);
            Assert.AreEqual(AttributeTargets.Field, usage.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
        }

        [Test]
        public void ConditionalAttribute_IsUnityEditor()
        {
            var conditional = (ConditionalAttribute)Attribute.GetCustomAttribute(
                typeof(MasterRefAttribute),
                typeof(ConditionalAttribute));
            Assert.NotNull(conditional);
            Assert.AreEqual("UNITY_EDITOR", conditional.ConditionString);
        }

        [Test]
        public void EditorReflection_ReadsMasterRefFromPrivateFields()
        {
            var itemId = typeof(TestRefRecord).GetField("itemId", BindingFlags.Instance | BindingFlags.NonPublic);
            var optional = typeof(TestRefRecord).GetField("optionalItemId", BindingFlags.Instance | BindingFlags.NonPublic);
            var questId = typeof(TestRefRecord).GetField("questId", BindingFlags.Instance | BindingFlags.NonPublic);

            var itemAttr = (MasterRefAttribute)itemId.GetCustomAttributes(typeof(MasterRefAttribute), false)[0];
            var optionalAttr = (MasterRefAttribute)optional.GetCustomAttributes(typeof(MasterRefAttribute), false)[0];
            var questAttr = (MasterRefAttribute)questId.GetCustomAttributes(typeof(MasterRefAttribute), false)[0];

            Assert.AreEqual(typeof(TestItemMaster), itemAttr.TargetMasterType);
            Assert.IsFalse(itemAttr.HasNoneValue);
            Assert.AreEqual(typeof(TestItemMaster), optionalAttr.TargetMasterType);
            Assert.IsTrue(optionalAttr.HasNoneValue);
            Assert.AreEqual(0, optionalAttr.NoneValue);
            Assert.AreEqual(typeof(TestQuestMaster), questAttr.TargetMasterType);
        }

        [Test]
        public void RuntimeAssembly_DoesNotReferenceUnityEditor()
        {
            var referenced = typeof(MasterRefAttribute).Assembly.GetReferencedAssemblies();
            Assert.IsFalse(referenced.Any(a => a.Name.StartsWith("UnityEditor")));
        }
    }
}
