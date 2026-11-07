using System;
using System.Reflection;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterToolWindow"/> のテストです。ウィンドウは開きません。
    /// </summary>
    public sealed class MasterToolWindowTests
    {
        [Test]
        public void CreateInstance_DoesNotRequireShowingTheWindow()
        {
            var window = ScriptableObject.CreateInstance<MasterToolWindow>();
            try
            {
                Assert.IsNotNull(window);
                Assert.IsTrue(typeof(EditorWindow).IsAssignableFrom(window.GetType()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void Open_HasImporterMenuItem()
        {
            var method = typeof(MasterToolWindow).GetMethod("Open", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);
            var menu = method.GetCustomAttribute<MenuItem>();
            Assert.IsNotNull(menu);
            Assert.AreEqual(MasterDataToolInfo.MenuRoot + "/Importer", menu.menuItem);
        }

        [Test]
        public void Type_IsPublicSealedEditorWindow()
        {
            Assert.IsTrue(typeof(MasterToolWindow).IsPublic);
            Assert.IsTrue(typeof(MasterToolWindow).IsSealed);
            Assert.IsTrue(typeof(EditorWindow).IsAssignableFrom(typeof(MasterToolWindow)));
        }

        [Test]
        public void StateField_IsSerialized()
        {
            var field = typeof(MasterToolWindow).GetField(
                "state",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            Assert.IsTrue(field.IsPrivate);
            Assert.IsNotNull(field.GetCustomAttribute<SerializeField>());
            Assert.AreEqual(typeof(MasterToolState), field.FieldType);
        }

        [Test]
        public void ExposedMembers_DoNotUseInternalTypes()
        {
            var members = typeof(MasterToolWindow).GetMembers(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            for (var i = 0; i < members.Length; i++)
            {
                if (!IsExposed(members[i]))
                {
                    continue;
                }

                AssertMemberTypesArePublic(members[i]);
            }
        }

        static bool IsExposed(MemberInfo member)
        {
            var method = member as MethodBase;
            if (method != null)
            {
                return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
            }

            var field = member as FieldInfo;
            if (field != null)
            {
                return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
            }

            var property = member as PropertyInfo;
            if (property != null)
            {
                return IsExposed(property.GetMethod) || IsExposed(property.SetMethod);
            }

            var evt = member as EventInfo;
            if (evt != null)
            {
                return IsExposed(evt.AddMethod);
            }

            return false;
        }

        static void AssertMemberTypesArePublic(MemberInfo member)
        {
            var method = member as MethodInfo;
            if (method != null)
            {
                AssertTypeIsPublic(method.ReturnType, method.Name);
                var parameters = method.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    AssertTypeIsPublic(parameters[i].ParameterType, method.Name);
                }

                return;
            }

            var property = member as PropertyInfo;
            if (property != null)
            {
                AssertTypeIsPublic(property.PropertyType, property.Name);
            }
        }

        static void AssertTypeIsPublic(Type type, string memberName)
        {
            if (type == null || type == typeof(void))
            {
                return;
            }

            if (type.IsByRef || type.IsPointer)
            {
                type = type.GetElementType();
            }

            if (type.IsArray)
            {
                AssertTypeIsPublic(type.GetElementType(), memberName);
                return;
            }

            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                for (var i = 0; i < arguments.Length; i++)
                {
                    AssertTypeIsPublic(arguments[i], memberName);
                }
            }

            Assert.IsTrue(
                type.IsPublic || type.IsNestedPublic,
                memberName + " exposes " + type.FullName);
        }
    }
}
