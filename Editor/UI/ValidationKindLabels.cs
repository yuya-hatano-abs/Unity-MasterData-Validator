using MasterDataTool.EditorTools.Validation;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// <see cref="ValidationIssueKind"/> の日本語名です。
    /// </summary>
    /// <remarks>
    /// 列挙値に定義されていない数値は、例外にせず、その数値の文字列を返します。
    /// </remarks>
    internal static class ValidationKindLabels
    {
        /// <summary>
        /// 種類の日本語名を返します。未定義の数値は、その数値の文字列です。
        /// </summary>
        /// <param name="kind">問題の種類です。</param>
        /// <returns>表示用の名前です。</returns>
        public static string GetLabel(ValidationIssueKind kind)
        {
            switch (kind)
            {
                case ValidationIssueKind.DuplicateId:
                    return "ID の重複";
                case ValidationIssueKind.BrokenReference:
                    return "参照切れ";
                case ValidationIssueKind.MissingRequiredField:
                    return "必須項目の欠落";
                case ValidationIssueKind.ImportFailed:
                    return "インポートの失敗";
                case ValidationIssueKind.TypeMismatch:
                    return "型の不一致";
                case ValidationIssueKind.ValueOutOfRange:
                    return "値の範囲外";
                case ValidationIssueKind.UnknownField:
                    return "未知のフィールド";
                case ValidationIssueKind.MissingField:
                    return "フィールドの欠落";
                case ValidationIssueKind.UnsupportedFieldType:
                    return "未対応のフィールド型";
                case ValidationIssueKind.ValidatorFailure:
                    return "検証器の異常";
                case ValidationIssueKind.ReferenceTargetUnavailable:
                    return "参照先なし";
                case ValidationIssueKind.InvalidReferenceDefinition:
                    return "参照定義の不正";
                case ValidationIssueKind.InvalidRequiredDefinition:
                    return "必須定義の不正";
                case ValidationIssueKind.MultipleMasterAssets:
                    return "アセットの重複";
                case ValidationIssueKind.AmbiguousMasterName:
                    return "マスタ名の曖昧さ";
                default:
                    return kind.ToString();
            }
        }
    }
}
