using System;
using System.IO;
using System.Linq;
using System.Text;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterCodeWriter"/> のテストです。
    /// </summary>
    public class MasterCodeWriterTests
    {
        string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "MasterCodeWriterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        static GeneratedCode CreateSampleCode()
        {
            var schema = new MasterSchema(MasterDataTool.Runtime.MasterIdKind.Int, new SchemaField[0]);
            var options = new MasterCodeOptions("Sample", "MyGame.MasterData");
            var generation = MasterCodeGenerator.Generate(schema, options);
            return generation.Code;
        }

        [Test]
        public void WriteToDirectory_WritesTwoFilesWithMatchingContent()
        {
            var code = CreateSampleCode();
            var result = MasterCodeWriter.WriteToDirectory(code, tempDirectory, OverwritePolicy.Overwrite);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.WrittenFiles.Count);
            Assert.AreEqual(File.ReadAllText(Path.Combine(tempDirectory, code.RecordFileName)), code.RecordSource);
            Assert.AreEqual(File.ReadAllText(Path.Combine(tempDirectory, code.MasterFileName)), code.MasterSource);
        }

        [Test]
        public void WriteToDirectory_UsesUtf8WithoutBomAndLf()
        {
            var schema = new MasterSchema(
                MasterDataTool.Runtime.MasterIdKind.Int,
                new[] { new SchemaField("name", SchemaFieldType.String, false, "Name", "string") });
            var generation = MasterCodeGenerator.Generate(schema, new MasterCodeOptions("Line", "MyGame.MasterData"));
            var result = MasterCodeWriter.WriteToDirectory(generation.Code, tempDirectory, OverwritePolicy.Overwrite);

            Assert.IsTrue(result.Succeeded);
            var bytes = File.ReadAllBytes(Path.Combine(tempDirectory, generation.Code.RecordFileName));
            Assert.AreNotEqual(0xEF, bytes[0]);
            Assert.IsFalse(generation.Code.RecordSource.Contains("\r"));
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes(generation.Code.RecordSource), bytes);
        }

        [Test]
        public void WriteToDirectory_CreatesDirectoryWhenMissing()
        {
            var nested = Path.Combine(tempDirectory, "nested", "dir");
            var result = MasterCodeWriter.WriteToDirectory(CreateSampleCode(), nested, OverwritePolicy.Overwrite);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(Directory.Exists(nested));
        }

        [Test]
        public void WriteToDirectory_FailPolicy_DoesNotOverwriteExisting()
        {
            var code = CreateSampleCode();
            var recordPath = Path.Combine(tempDirectory, code.RecordFileName);
            File.WriteAllText(recordPath, "existing");
            var original = File.ReadAllText(recordPath);

            var result = MasterCodeWriter.WriteToDirectory(code, tempDirectory, OverwritePolicy.Fail);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.FileExists));
            Assert.AreEqual(original, File.ReadAllText(recordPath));
            Assert.IsFalse(File.Exists(Path.Combine(tempDirectory, code.MasterFileName)));
        }

        [Test]
        public void WriteToDirectory_OverwritePolicy_ReplacesExisting()
        {
            var code = CreateSampleCode();
            var recordPath = Path.Combine(tempDirectory, code.RecordFileName);
            File.WriteAllText(recordPath, "existing");

            var result = MasterCodeWriter.WriteToDirectory(code, tempDirectory, OverwritePolicy.Overwrite);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(code.RecordSource, File.ReadAllText(recordPath));
        }

        [Test]
        public void WriteToDirectory_WhenFilePathIsDirectory_ReturnsIoError()
        {
            var code = CreateSampleCode();
            Directory.CreateDirectory(Path.Combine(tempDirectory, code.RecordFileName));

            var result = MasterCodeWriter.WriteToDirectory(code, tempDirectory, OverwritePolicy.Overwrite);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.IoError));
        }

        [Test]
        public void WriteToAssetFolder_InvalidPaths_DoNotWrite()
        {
            var code = CreateSampleCode();
            Assert.IsFalse(MasterCodeWriter.WriteToAssetFolder(code, string.Empty, OverwritePolicy.Overwrite).Succeeded);
            Assert.IsFalse(MasterCodeWriter.WriteToAssetFolder(code, "Packages/Foo", OverwritePolicy.Overwrite).Succeeded);
            Assert.IsFalse(MasterCodeWriter.WriteToAssetFolder(code, "Assets/../Outside", OverwritePolicy.Overwrite).Succeeded);
            Assert.IsFalse(MasterCodeWriter.WriteToAssetFolder(code, tempDirectory, OverwritePolicy.Overwrite).Succeeded);
        }
    }
}
