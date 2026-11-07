using System.IO;
using MasterDataTool.EditorTools.Importing;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="AssetPathValidator"/> のテストです。
    /// </summary>
    public sealed class AssetPathValidatorTests
    {
        [Test]
        public void TryValidate_InvalidPaths_FailWithReason()
        {
            AssertInvalid(null);
            AssertInvalid(string.Empty);
            AssertInvalid("Packages/x/y.asset");
            AssertInvalid("Assets/../outside.asset");
            AssertInvalid("Assets/foo.txt");
            AssertInvalid("Assets/foo\\bar.asset");
            AssertInvalid("Assets/foo/");
            AssertInvalid("Assets//foo.asset");
            AssertInvalid("Assets/.asset");
            AssertInvalid(Path.GetFullPath(Path.Combine("Assets", "x.asset")));
        }

        [Test]
        public void TryValidate_ValidPath_Succeeds()
        {
            var valid = AssetPathValidator.TryValidate("Assets/MasterData/Item.asset", out var reason);

            Assert.IsTrue(valid);
            Assert.IsNull(reason);
        }

        static void AssertInvalid(string assetPath)
        {
            var valid = AssetPathValidator.TryValidate(assetPath, out var reason);

            Assert.IsFalse(valid, assetPath ?? "<null>");
            Assert.IsFalse(string.IsNullOrEmpty(reason), assetPath ?? "<null>");
        }
    }
}
