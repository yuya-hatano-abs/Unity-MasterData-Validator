using System.Linq;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// Runtime assembly が Player 向けに正しく構成されていることを検証します。
    /// </summary>
    public class RuntimeAssemblyTests
    {
        const string RuntimeAssemblyName = "MasterDataTool.Runtime";
        const string EditorAssemblyName = "MasterDataTool.Editor";

        [Test]
        public void PlayerAssemblies_IncludeMasterDataToolRuntime()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            Assert.IsTrue(playerAssemblies.Any(a => a.name == RuntimeAssemblyName));
        }

        [Test]
        public void PlayerAssemblies_DoNotIncludeMasterDataToolEditor()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            Assert.IsFalse(playerAssemblies.Any(a => a.name == EditorAssemblyName));
        }

        [Test]
        public void MasterDataBaseAssembly_DoesNotReferenceUnityEditor()
        {
            var referenced = typeof(MasterDataBase).Assembly.GetReferencedAssemblies();
            Assert.IsFalse(referenced.Any(a => a.Name.StartsWith("UnityEditor")));
        }
    }
}
