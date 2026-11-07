using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ProjectImportRunner"/> のテストです。
    /// </summary>
    public sealed class ProjectImportRunnerTests
    {
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        string jsonPath;

        [SetUp]
        public void SetUp()
        {
            jsonPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "mdt-runner-" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(owned[i]);
                }
            }

            owned.Clear();
            if (!string.IsNullOrEmpty(jsonPath) && System.IO.File.Exists(jsonPath))
            {
                System.IO.File.Delete(jsonPath);
            }

            jsonPath = null;
        }

        [Test]
        public void Run_Validate_SucceedsWithFakeSource()
        {
            System.IO.File.WriteAllText(
                jsonPath,
                "{ \"records\": [ { \"id\": 1, \"name\": \"Potion\" } ] }");
            var confirm = new FakeConfirm();
            var source = new FakeSource();
            source.Assets.Add(OwnItem(7, "other"));
            var runner = new ProjectImportRunner(
                new ProjectMasterTableCollector(source),
                new MasterImportPipeline(confirm));

            var result = runner.Run(new PipelineRequest(
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                jsonPath,
                "Assets/MasterData/TestItemMaster.asset",
                PipelineMode.Validate));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            Assert.IsNull(result.Save);
            Assert.AreEqual(0, confirm.Calls);
            Assert.AreEqual(1, result.Table.Records.Count);
        }

        [Test]
        public void Run_DuplicateProjectAssets_IncludesCollectorWarning()
        {
            System.IO.File.WriteAllText(
                jsonPath,
                "{ \"records\": [ { \"id\": \"Quest_A\", \"title\": \"First\" } ] }");
            var source = new FakeSource();
            source.Assets.Add(OwnItem(1, "a"));
            source.Assets.Add(OwnItem(2, "b"));
            var runner = new ProjectImportRunner(
                new ProjectMasterTableCollector(source),
                new MasterImportPipeline(new FakeConfirm()));

            var result = runner.Run(new PipelineRequest(
                typeof(TestQuestMaster),
                typeof(TestQuestRecord),
                jsonPath,
                "Assets/MasterData/TestQuestMaster.asset",
                PipelineMode.Validate));

            Assert.AreEqual(PipelineStatus.Validated, result.Status);
            Assert.IsTrue(result.CanSave);
            Assert.IsTrue(ContainsKind(result.Validation, ValidationIssueKind.MultipleMasterAssets));
            Assert.AreEqual("TestItemMaster", FindKind(result.Validation, ValidationIssueKind.MultipleMasterAssets).MasterName);
        }

        [Test]
        public void Constructor_Parameterless_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new ProjectImportRunner());
        }

        [Test]
        public void Constructor_DoesNotCollect()
        {
            var source = new FlagSource();
            var runner = new ProjectImportRunner(
                new ProjectMasterTableCollector(source),
                new MasterImportPipeline(new FakeConfirm()));

            Assert.IsNotNull(runner);
            Assert.AreEqual(0, source.Calls);
        }

        TestItemMaster OwnItem(int id, string name)
        {
            var asset = ScriptableObject.CreateInstance<TestItemMaster>();
            asset.ReplaceRecords(new[] { new TestItemRecord(id, name) });
            owned.Add(asset);
            return asset;
        }

        static bool ContainsKind(ValidationResult validation, ValidationIssueKind kind)
        {
            return FindKind(validation, kind) != null;
        }

        static ValidationIssue FindKind(ValidationResult validation, ValidationIssueKind kind)
        {
            var issues = validation.Issues;
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Kind == kind)
                {
                    return issues[i];
                }
            }

            return null;
        }

        sealed class FakeSource : IMasterAssetSource
        {
            public readonly List<MasterDataBase> Assets = new List<MasterDataBase>();

            public IReadOnlyList<MasterDataBase> LoadAll()
            {
                return Assets;
            }
        }

        sealed class FlagSource : IMasterAssetSource
        {
            public int Calls;

            public IReadOnlyList<MasterDataBase> LoadAll()
            {
                Calls++;
                return new MasterDataBase[0];
            }
        }

        sealed class FakeConfirm : IOverwriteConfirmation
        {
            public int Calls;

            public bool Confirm(string assetPath, int previousRecordCount, int newRecordCount, string diffSummary)
            {
                Calls++;
                return true;
            }
        }
    }
}
