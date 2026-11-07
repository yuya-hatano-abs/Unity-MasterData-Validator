using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="IssueRowFormatter"/> のテストです。
    /// </summary>
    public sealed class IssueRowFormatterTests
    {
        [Test]
        public void FormatPrimary_PrefixesSeverity()
        {
            var error = Issue(ValidationSeverity.Error, ValidationIssueKind.DuplicateId, "重複しています。");
            var warning = Issue(ValidationSeverity.Warning, ValidationIssueKind.UnknownField, "未知です。");

            Assert.AreEqual("[エラー] 重複しています。", IssueRowFormatter.FormatPrimary(error));
            Assert.AreEqual("[警告] 未知です。", IssueRowFormatter.FormatPrimary(warning));
        }

        [Test]
        public void FormatDetail_FullLine()
        {
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "duplicate-id",
                "重複しています。",
                "ItemMaster",
                "Assets/ItemMaster.asset",
                3,
                "5",
                "id",
                12);

            Assert.AreEqual(
                "ID の重複 ・ ItemMaster ・ records[3] ・ ID: 5 ・ フィールド: id ・ JSON 12 行目",
                IssueRowFormatter.FormatDetail(issue));
        }

        [Test]
        public void FormatDetail_OmitsMissingParts()
        {
            var partial = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.MissingRequiredField,
                "required",
                "名前が空です。",
                fieldName: "name");
            var kindOnly = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.ImportFailed,
                "import",
                "読めません。");

            Assert.AreEqual("必須項目の欠落 ・ フィールド: name", IssueRowFormatter.FormatDetail(partial));
            Assert.AreEqual("インポートの失敗", IssueRowFormatter.FormatDetail(kindOnly));
            Assert.IsFalse(IssueRowFormatter.FormatDetail(kindOnly).Contains(" ・ "));
        }

        [Test]
        public void FormatDetail_RecordIndexZeroAndNegative()
        {
            var zero = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "duplicate-id",
                "先頭です。",
                recordIndex: 0);
            var missing = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "duplicate-id",
                "位置なしです。",
                recordIndex: -1);

            Assert.AreEqual("ID の重複 ・ records[0]", IssueRowFormatter.FormatDetail(zero));
            Assert.AreEqual("ID の重複", IssueRowFormatter.FormatDetail(missing));
            Assert.IsFalse(IssueRowFormatter.FormatDetail(missing).Contains("records["));
        }

        [Test]
        public void FormatTooltip_IncludesAssetAndValidator()
        {
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.DuplicateId,
                "duplicate-id",
                "重複しています。",
                assetPath: "Assets/ItemMaster.asset");

            Assert.AreEqual(
                "重複しています。\nアセット: Assets/ItemMaster.asset\n検証器: duplicate-id",
                IssueRowFormatter.FormatTooltip(issue));
        }

        [Test]
        public void FormatTooltip_OmitsAssetWhenPathMissing()
        {
            var noPath = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.UnknownField,
                "import",
                "未知です。");
            var emptyPath = new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.UnknownField,
                "import",
                "未知です。",
                assetPath: string.Empty);

            Assert.AreEqual("未知です。\n検証器: import", IssueRowFormatter.FormatTooltip(noPath));
            Assert.AreEqual("未知です。\n検証器: import", IssueRowFormatter.FormatTooltip(emptyPath));
            Assert.IsFalse(IssueRowFormatter.FormatTooltip(noPath).Contains("アセット:"));
        }

        [Test]
        public void Format_NullIssue_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => IssueRowFormatter.FormatPrimary(null));
            Assert.Throws<System.ArgumentNullException>(() => IssueRowFormatter.FormatDetail(null));
            Assert.Throws<System.ArgumentNullException>(() => IssueRowFormatter.FormatTooltip(null));
        }

        static ValidationIssue Issue(ValidationSeverity severity, ValidationIssueKind kind, string message)
        {
            return new ValidationIssue(severity, kind, "test", message);
        }
    }
}
