using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.UI;
using MasterDataTool.EditorTools.Validation;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationKindLabels"/> のテストです。
    /// </summary>
    public sealed class ValidationKindLabelsTests
    {
        [Test]
        public void GetLabel_AllDefinedValues_AreNonEmpty()
        {
            var values = (ValidationIssueKind[])Enum.GetValues(typeof(ValidationIssueKind));
            Assert.Greater(values.Length, 0);
            for (var i = 0; i < values.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(ValidationKindLabels.GetLabel(values[i])));
            }
        }

        [Test]
        public void GetLabel_KnownValues_MatchJapaneseNames()
        {
            // この表に無い将来の列挙値は、ToString() と同じになってよい。
            var expected = new Dictionary<ValidationIssueKind, string>
            {
                { ValidationIssueKind.DuplicateId, "ID の重複" },
                { ValidationIssueKind.BrokenReference, "参照切れ" },
                { ValidationIssueKind.MissingRequiredField, "必須項目の欠落" },
                { ValidationIssueKind.ImportFailed, "インポートの失敗" },
                { ValidationIssueKind.TypeMismatch, "型の不一致" },
                { ValidationIssueKind.ValueOutOfRange, "値の範囲外" },
                { ValidationIssueKind.UnknownField, "未知のフィールド" },
                { ValidationIssueKind.MissingField, "フィールドの欠落" },
                { ValidationIssueKind.UnsupportedFieldType, "未対応のフィールド型" },
                { ValidationIssueKind.ValidatorFailure, "検証器の異常" },
                { ValidationIssueKind.ReferenceTargetUnavailable, "参照先なし" },
                { ValidationIssueKind.InvalidReferenceDefinition, "参照定義の不正" },
                { ValidationIssueKind.InvalidRequiredDefinition, "必須定義の不正" },
                { ValidationIssueKind.MultipleMasterAssets, "アセットの重複" },
                { ValidationIssueKind.AmbiguousMasterName, "マスタ名の曖昧さ" },
            };

            var values = (ValidationIssueKind[])Enum.GetValues(typeof(ValidationIssueKind));
            for (var i = 0; i < values.Length; i++)
            {
                var kind = values[i];
                if (expected.ContainsKey(kind))
                {
                    Assert.AreEqual(expected[kind], ValidationKindLabels.GetLabel(kind));
                }
                else
                {
                    Assert.AreEqual(kind.ToString(), ValidationKindLabels.GetLabel(kind));
                }
            }

            Assert.AreEqual(15, expected.Count);
        }

        [Test]
        public void GetLabel_UnknownValue_ReturnsNumericName()
        {
            Assert.AreEqual("9999", ValidationKindLabels.GetLabel((ValidationIssueKind)9999));
        }
    }
}
