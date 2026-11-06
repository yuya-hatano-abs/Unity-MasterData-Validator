using System;
using System.IO;
using System.Linq;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Tests.Golden;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// golden ファイルとジェネレーター出力の一致を検証します。
    /// </summary>
    public class GoldenGenerationTests
    {
        const string GoldenItemJson =
            "{\n" +
            "  \"records\": [\n" +
            "    { \"id\": 1, \"name\": \"Potion\", \"price\": 100, \"ratio\": 1.5, \"enabled\": true, \"bigCount\": 100, \"tags\": [\"heal\"] },\n" +
            "    { \"id\": 2, \"name\": \"Ether\", \"price\": 250, \"ratio\": 2.25, \"enabled\": false, \"bigCount\": 5000000000, \"tags\": [\"mp\"] }\n" +
            "  ]\n" +
            "}";

        const string GoldenQuestJson =
            "{\n" +
            "  \"records\": [\n" +
            "    { \"id\": \"Q001\", \"title\": \"First Quest\", \"rewards\": [10, 20] },\n" +
            "    { \"id\": \"Q002\", \"title\": \"Second Quest\", \"rewards\": [30] }\n" +
            "  ]\n" +
            "}";

        const string GoldenNamespace = "MasterDataTool.EditorTools.Tests.Golden";

        const string GoldenRecordRelativePath = "Tests/Editor/TestData/Golden/GoldenItemRecord.cs";

        static string PackageRoot
        {
            get
            {
                var guids = AssetDatabase.FindAssets("GoldenItemRecord t:MonoScript");
                for (var i = 0; i < guids.Length; i++)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                    if (!assetPath.EndsWith(GoldenRecordRelativePath, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var packageRelativeRoot = assetPath.Substring(
                        0,
                        assetPath.Length - GoldenRecordRelativePath.Length);
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    var resolvedRoot = Path.GetFullPath(Path.Combine(projectRoot, packageRelativeRoot));
                    var goldenPath = Path.Combine(
                        resolvedRoot,
                        GoldenRecordRelativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(goldenPath))
                    {
                        Assert.Fail("golden ファイルが見つかりません: " + goldenPath);
                    }

                    return resolvedRoot;
                }

                Assert.Fail("GoldenItemRecord の MonoScript が AssetDatabase 上で見つかりませんでした。");
                return null;
            }
        }

        static string ReadGoldenFile(string relativePath)
        {
            var fullPath = Path.Combine(PackageRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                Assert.Fail("golden ファイルが見つかりません: " + fullPath);
            }

            return NormalizeLineEndings(File.ReadAllText(fullPath));
        }

        static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        static void AssertSourceMatchesGolden(string expectedRelativePath, string actualSource)
        {
            var expected = ReadGoldenFile(expectedRelativePath);
            var actual = NormalizeLineEndings(actualSource);
            if (expected == actual)
            {
                return;
            }

            var expectedLines = expected.Split('\n');
            var actualLines = actual.Split('\n');
            var max = Math.Max(expectedLines.Length, actualLines.Length);
            for (var i = 0; i < max; i++)
            {
                var expectedLine = i < expectedLines.Length ? expectedLines[i] : "<EOF>";
                var actualLine = i < actualLines.Length ? actualLines[i] : "<EOF>";
                if (expectedLine != actualLine)
                {
                    Assert.Fail(
                        "行 " + (i + 1) + " が一致しません。\n期待: " + expectedLine + "\n実際: " + actualLine);
                }
            }
        }

        [Test]
        public void Generate_GoldenItem_MatchesGoldenFiles()
        {
            var options = new MasterCodeOptions("GoldenItem", GoldenNamespace);
            var result = MasterCodeGenerator.GenerateFromJson(GoldenItemJson, options);

            Assert.IsTrue(result.Succeeded, string.Join("\n", result.Issues.Select(i => i.Message)));
            AssertSourceMatchesGolden("Tests/Editor/TestData/Golden/GoldenItemRecord.cs", result.Code.RecordSource);
            AssertSourceMatchesGolden("Tests/Editor/TestData/Golden/GoldenItemMaster.cs", result.Code.MasterSource);
        }

        [Test]
        public void Generate_GoldenQuest_MatchesGoldenFiles()
        {
            var options = new MasterCodeOptions("GoldenQuest", GoldenNamespace);
            var result = MasterCodeGenerator.GenerateFromJson(GoldenQuestJson, options);

            Assert.IsTrue(result.Succeeded);
            AssertSourceMatchesGolden("Tests/Editor/TestData/Golden/GoldenQuestRecord.cs", result.Code.RecordSource);
            AssertSourceMatchesGolden("Tests/Editor/TestData/Golden/GoldenQuestMaster.cs", result.Code.MasterSource);
        }

        [Test]
        public void Import_GoldenItemJson_SucceedsWithExpectedValues()
        {
            var result = MasterJsonImporter.Import<GoldenItemRecord>(GoldenItemJson);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Issues.Count);
            Assert.AreEqual(2, result.Records.Count);
            Assert.AreEqual(1, result.Records[0].Id);
            Assert.AreEqual("Potion", result.Records[0].Name);
            Assert.AreEqual(100, result.Records[0].Price);
            Assert.AreEqual(1.5f, result.Records[0].Ratio);
            Assert.IsTrue(result.Records[0].Enabled);
            Assert.AreEqual(100L, result.Records[0].BigCount);
            CollectionAssert.AreEqual(new[] { "heal" }, result.Records[0].Tags);
            Assert.AreEqual(5000000000L, result.Records[1].BigCount);
        }

        [Test]
        public void Import_GoldenQuestJson_SucceedsWithExpectedValues()
        {
            var result = MasterJsonImporter.Import<GoldenQuestRecord>(GoldenQuestJson);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Issues.Count);
            Assert.AreEqual("Q001", result.Records[0].Id);
            Assert.AreEqual("First Quest", result.Records[0].Title);
            CollectionAssert.AreEqual(new[] { 10, 20 }, result.Records[0].Rewards);
        }

        [Test]
        public void GoldenMasters_IdKind_AndCreateInstance()
        {
            Assert.AreEqual(MasterIdKind.Int, ScriptableObject.CreateInstance<GoldenItemMaster>().IdKind);
            Assert.AreEqual(MasterIdKind.String, ScriptableObject.CreateInstance<GoldenQuestMaster>().IdKind);

            var itemMaster = ScriptableObject.CreateInstance<GoldenItemMaster>();
            var questMaster = ScriptableObject.CreateInstance<GoldenQuestMaster>();
            UnityEngine.Object.DestroyImmediate(itemMaster);
            UnityEngine.Object.DestroyImmediate(questMaster);
        }
    }
}
