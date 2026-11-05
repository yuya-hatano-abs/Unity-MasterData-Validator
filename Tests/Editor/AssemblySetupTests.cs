using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// Task 0 で追加した assembly 構成が設計どおりであることを検証します。
    /// </summary>
    public class AssemblySetupTests
    {
        const string EditorAssemblyName = "MasterDataTool.Editor";

        [Test]
        public void EditorAssembly_HasExpectedName()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var masterDataEditor = editorAssemblies.FirstOrDefault(a => a.name == EditorAssemblyName);

            Assert.IsNotNull(masterDataEditor, $"Editor assembly \"{EditorAssemblyName}\" was not found.");
        }

        [Test]
        public void PlayerAssemblies_DoNotIncludeEditorAssembly()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var included = playerAssemblies.Any(a => a.name == EditorAssemblyName);

            Assert.IsFalse(included, $"Player assemblies must not include \"{EditorAssemblyName}\".");
        }

        [Test]
        public void MasterDataToolInfo_ConstantsAreNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(MasterDataToolInfo.ToolName));
            Assert.IsFalse(string.IsNullOrEmpty(MasterDataToolInfo.Version));
            Assert.IsFalse(string.IsNullOrEmpty(MasterDataToolInfo.MenuRoot));
            Assert.IsFalse(string.IsNullOrEmpty(MasterDataToolInfo.SettingsKeyPrefix));
        }
    }
}
