using System;
using System.Collections.Generic;
using System.Linq;
using MasterDataTool.EditorTools.UI;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterToolModel"/> のテストです。
    /// </summary>
    public sealed class MasterToolModelTests
    {
        [Test]
        public void Validate_EmptyState_ReportsThreeErrorsInOrder()
        {
            var fixture = new ModelFixture();

            var problems = fixture.Model.Validate();

            Assert.AreEqual(3, problems.Count);
            Assert.AreEqual(InputField.MasterType, problems[0].Field);
            Assert.AreEqual(InputField.JsonPath, problems[1].Field);
            Assert.AreEqual(InputField.AssetPath, problems[2].Field);
            Assert.AreEqual(InputProblemSeverity.Error, problems[0].Severity);
            Assert.AreEqual(InputProblemSeverity.Error, problems[1].Severity);
            Assert.AreEqual(InputProblemSeverity.Error, problems[2].Severity);
            Assert.AreEqual("対象のマスタ型を選択してください。", problems[0].Message);
            Assert.AreEqual("JSON ファイルを指定してください。", problems[1].Message);
            Assert.AreEqual("保存先のアセットパスを指定してください。", problems[2].Message);
            Assert.IsFalse(fixture.Model.CanProceed);
        }

        [Test]
        public void SelectType_SetsTypeAndSuggestsAssetPathWhenEmpty()
        {
            var fixture = new ModelFixture();
            var info = Require(fixture, typeof(TestItemMaster));

            fixture.Model.SelectType(info);

            Assert.AreEqual(typeof(TestItemMaster).FullName, fixture.State.SelectedTypeName);
            Assert.AreEqual(typeof(TestItemMaster), fixture.Model.SelectedType.MasterType);
            Assert.AreEqual("Assets/MasterData/TestItemMaster.asset", fixture.State.AssetPath);
        }

        [Test]
        public void SelectType_DoesNotOverwriteExistingAssetPath()
        {
            var fixture = new ModelFixture();
            fixture.State.AssetPath = "Assets/Custom/Item.asset";

            fixture.Model.SelectType(Require(fixture, typeof(TestItemMaster)));

            Assert.AreEqual("Assets/Custom/Item.asset", fixture.State.AssetPath);
        }

        [Test]
        public void SelectType_WhitespaceAssetPath_UsesSuggestion()
        {
            var fixture = new ModelFixture();
            fixture.State.AssetPath = "   ";

            fixture.Model.SelectType(Require(fixture, typeof(TestItemMaster)));

            Assert.AreEqual("Assets/MasterData/TestItemMaster.asset", fixture.State.AssetPath);
        }

        [Test]
        public void SelectType_Null_ClearsTypeAndKeepsAssetPath()
        {
            var fixture = new ModelFixture();
            fixture.State.AssetPath = "Assets/Custom/Item.asset";
            fixture.State.SelectedTypeName = typeof(TestItemMaster).FullName;

            fixture.Model.SelectType(null);

            Assert.AreEqual(string.Empty, fixture.State.SelectedTypeName);
            Assert.IsNull(fixture.Model.SelectedType);
            Assert.AreEqual("Assets/Custom/Item.asset", fixture.State.AssetPath);
        }

        [Test]
        public void SelectedType_UnknownName_IsNullAndReportsMissing()
        {
            var fixture = new ModelFixture();
            fixture.State.SelectedTypeName = "Missing.Type";

            Assert.IsNull(fixture.Model.SelectedType);
            var problem = FindField(fixture.Model.Validate(), InputField.MasterType);
            Assert.AreEqual(InputProblemSeverity.Error, problem.Severity);
            StringAssert.Contains("見つかりません", problem.Message);
            StringAssert.Contains("Missing.Type", problem.Message);
            StringAssert.Contains("再読み込み", problem.Message);
        }

        [Test]
        public void SetPaths_NullBecomesEmpty()
        {
            var fixture = new ModelFixture();

            fixture.Model.SetJsonPath(null);
            fixture.Model.SetAssetPath(null);

            Assert.AreEqual(string.Empty, fixture.State.JsonPath);
            Assert.AreEqual(string.Empty, fixture.State.AssetPath);
        }

        [Test]
        public void Validate_JsonPath_EmptyOrWhitespace()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Model.SetJsonPath("   ");

            var problem = FindField(fixture.Model.Validate(), InputField.JsonPath);

            Assert.AreEqual(InputProblemSeverity.Error, problem.Severity);
            Assert.AreEqual("JSON ファイルを指定してください。", problem.Message);
        }

        [Test]
        public void Validate_JsonPath_WrongExtension()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Model.SetJsonPath("C:/data/items.txt");

            var problem = FindField(fixture.Model.Validate(), InputField.JsonPath);

            Assert.AreEqual("JSON ファイル(.json)を指定してください。", problem.Message);
        }

        [Test]
        public void Validate_JsonPath_MissingFile()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Model.SetJsonPath("C:/data/missing.json");

            var problem = FindField(fixture.Model.Validate(), InputField.JsonPath);

            Assert.AreEqual(InputProblemSeverity.Error, problem.Severity);
            StringAssert.Contains("JSON ファイルが見つかりません:", problem.Message);
            StringAssert.Contains("C:/data/missing.json", problem.Message);
        }

        [Test]
        public void Validate_JsonPath_UppercaseExtension_WhenFileExists_IsValid()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Model.SetJsonPath("C:/data/items.JSON");
            fixture.ExistingFiles.Add("C:/data/items.JSON");

            Assert.IsNull(FindFieldOrNull(fixture.Model.Validate(), InputField.JsonPath));
        }

        [Test]
        public void Validate_AssetPath_EmptyAndInvalid()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);

            fixture.Model.SetAssetPath("  ");
            var empty = FindField(fixture.Model.Validate(), InputField.AssetPath);
            Assert.AreEqual("保存先のアセットパスを指定してください。", empty.Message);

            fixture.Model.SetAssetPath("Packages/x/y.asset");
            var outside = FindField(fixture.Model.Validate(), InputField.AssetPath);
            Assert.AreEqual(InputProblemSeverity.Error, outside.Severity);
            Assert.IsFalse(string.IsNullOrEmpty(outside.Message));

            fixture.Model.SetAssetPath("Assets/foo\\bar.asset");
            var backslash = FindField(fixture.Model.Validate(), InputField.AssetPath);
            Assert.AreEqual(InputProblemSeverity.Error, backslash.Severity);
            StringAssert.Contains("バックスラッシュ", backslash.Message);
        }

        [Test]
        public void Validate_AssetPath_ValidMissingAsset_HasNoProblem()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);

            Assert.IsNull(FindFieldOrNull(fixture.Model.Validate(), InputField.AssetPath));
            Assert.IsTrue(fixture.Model.CanProceed);
        }

        [Test]
        public void Validate_AssetPath_CompatibleExisting_IsInfo()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Lookup = _ => typeof(TestItemMaster);

            var problems = fixture.Model.Validate();
            var problem = FindField(problems, InputField.AssetPath);

            Assert.AreEqual(1, problems.Count);
            Assert.AreEqual(InputProblemSeverity.Info, problem.Severity);
            Assert.AreEqual("既存のアセットを更新します。", problem.Message);
            Assert.IsTrue(fixture.Model.CanProceed);
        }

        [Test]
        public void Validate_AssetPath_IncompatibleExisting_IsError()
        {
            var fixture = new ModelFixture();
            PrepareValid(fixture);
            fixture.Lookup = _ => typeof(TestQuestMaster);

            var problem = FindField(fixture.Model.Validate(), InputField.AssetPath);

            Assert.AreEqual(InputProblemSeverity.Error, problem.Severity);
            StringAssert.Contains("別の型のアセット", problem.Message);
            StringAssert.Contains("TestQuestMaster", problem.Message);
            Assert.IsFalse(fixture.Model.CanProceed);
        }

        [Test]
        public void Validate_AssetPath_UnselectedType_SkipsCompatibility()
        {
            var fixture = new ModelFixture();
            fixture.Model.SetJsonPath("C:/data/items.json");
            fixture.ExistingFiles.Add("C:/data/items.json");
            fixture.Model.SetAssetPath("Assets/MasterData/TestItemMaster.asset");
            fixture.Lookup = _ => typeof(TestQuestMaster);

            var problems = fixture.Model.Validate();

            Assert.IsNull(FindFieldOrNull(problems, InputField.AssetPath));
            Assert.IsNotNull(FindField(problems, InputField.MasterType));
            Assert.IsFalse(problems.Any(problem => problem.Severity == InputProblemSeverity.Info));
        }

        [Test]
        public void Constructor_NullStateOrCatalog_Throws()
        {
            var catalog = new MasterTypeCatalog(type => type == typeof(TestItemMaster));

            Assert.Throws<ArgumentNullException>(() => new MasterToolModel(null, catalog));
            Assert.Throws<ArgumentNullException>(() => new MasterToolModel(new MasterToolState(), null));
        }

        static void PrepareValid(ModelFixture fixture)
        {
            fixture.Model.SelectType(Require(fixture, typeof(TestItemMaster)));
            fixture.Model.SetJsonPath("C:/data/items.json");
            fixture.ExistingFiles.Add("C:/data/items.json");
        }

        static MasterTypeInfo Require(ModelFixture fixture, Type masterType)
        {
            Assert.IsTrue(fixture.Catalog.TryFind(masterType.FullName, out var info));
            return info;
        }

        static InputProblem FindField(IReadOnlyList<InputProblem> problems, InputField field)
        {
            var found = FindFieldOrNull(problems, field);
            Assert.IsNotNull(found);
            return found;
        }

        static InputProblem FindFieldOrNull(IReadOnlyList<InputProblem> problems, InputField field)
        {
            for (var i = 0; i < problems.Count; i++)
            {
                if (problems[i].Field == field)
                {
                    return problems[i];
                }
            }

            return null;
        }

        sealed class ModelFixture
        {
            public readonly HashSet<string> ExistingFiles = new HashSet<string>(StringComparer.Ordinal);
            public Func<string, Type> Lookup = _ => null;
            public readonly MasterToolState State = new MasterToolState();
            public readonly MasterTypeCatalog Catalog;
            public readonly MasterToolModel Model;

            public ModelFixture()
            {
                Catalog = new MasterTypeCatalog(type =>
                    type == typeof(TestItemMaster) || type == typeof(TestQuestMaster));
                Model = new MasterToolModel(State, Catalog, ExistingFiles.Contains, path => Lookup(path));
            }
        }
    }
}
