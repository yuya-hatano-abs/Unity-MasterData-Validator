using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterRequiredAttribute"/> のテストです。
    /// </summary>
    public class MasterRequiredAttributeTests
    {
        [Test]
        public void AttributeUsage_FieldOnlyAllowMultipleFalse()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(MasterRequiredAttribute),
                typeof(AttributeUsageAttribute));
            Assert.NotNull(usage);
            Assert.AreEqual(AttributeTargets.Field, usage.ValidOn);
            Assert.IsFalse(usage.AllowMultiple);
        }

        [Test]
        public void ConditionalAttribute_IsUnityEditor()
        {
            var conditional = (ConditionalAttribute)Attribute.GetCustomAttribute(
                typeof(MasterRequiredAttribute),
                typeof(ConditionalAttribute));
            Assert.NotNull(conditional);
            Assert.AreEqual("UNITY_EDITOR", conditional.ConditionString);
        }

        [Test]
        public void EditorReflection_ReadsMasterRequiredFromPrivateFields()
        {
            var name = typeof(TestRequiredRecord).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic);
            var note = typeof(TestRequiredRecord).GetField("note", BindingFlags.Instance | BindingFlags.NonPublic);
            var questId = typeof(TestRequiredRecord).GetField(
                "questId",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.AreEqual(1, name.GetCustomAttributes(typeof(MasterRequiredAttribute), false).Length);
            Assert.AreEqual(0, note.GetCustomAttributes(typeof(MasterRequiredAttribute), false).Length);
            Assert.AreEqual(1, questId.GetCustomAttributes(typeof(MasterRequiredAttribute), false).Length);
            Assert.AreEqual(1, questId.GetCustomAttributes(typeof(MasterRefAttribute), false).Length);
        }

        [Test]
        public void RuntimeAssembly_DoesNotReferenceUnityEditor()
        {
            var referenced = typeof(MasterRequiredAttribute).Assembly.GetReferencedAssemblies();
            Assert.IsFalse(referenced.Any(a => a.Name.StartsWith("UnityEditor")));
        }
    }
}
