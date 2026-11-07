using System;
using System.IO;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// 保存先アセットパスの形式を検査します。
    /// </summary>
    internal static class AssetPathValidator
    {
        /// <summary>
        /// アセットパスが保存に使える形式かを検査します。
        /// </summary>
        /// <param name="assetPath">検査するパスです。</param>
        /// <param name="reason">不正な場合の理由です。有効な場合は null です。</param>
        /// <returns>有効な場合は true です。</returns>
        public static bool TryValidate(string assetPath, out string reason)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                reason = "アセットパスが空です。";
                return false;
            }

            if (Path.IsPathRooted(assetPath))
            {
                reason = "アセットパスは Assets/ で始まるプロジェクト相対パスである必要があります。";
                return false;
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                reason = "アセットパスは Assets/ で始まる必要があります。";
                return false;
            }

            if (assetPath.Contains("\\"))
            {
                reason = "アセットパスにバックスラッシュを含めることはできません。";
                return false;
            }

            if (assetPath.EndsWith("/", StringComparison.Ordinal))
            {
                reason = "アセットパスをスラッシュで終えることはできません。";
                return false;
            }

            if (assetPath.IndexOf("//", StringComparison.Ordinal) >= 0)
            {
                reason = "アセットパスに連続したスラッシュを含めることはできません。";
                return false;
            }

            if (!assetPath.EndsWith(".asset", StringComparison.Ordinal))
            {
                reason = "アセットパスは .asset で終わる必要があります。";
                return false;
            }

            var fileName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(fileName) ||
                string.Equals(fileName, ".asset", StringComparison.Ordinal))
            {
                reason = "アセットのファイル名が空です。";
                return false;
            }

            var segments = assetPath.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[i]))
                {
                    reason = "アセットパスに空のセグメントが含まれています。";
                    return false;
                }

                if (string.Equals(segments[i], "..", StringComparison.Ordinal))
                {
                    reason = "アセットパスに .. を含めることはできません。";
                    return false;
                }
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < assetPath.Length; i++)
            {
                var c = assetPath[i];
                if (c == '/')
                {
                    continue;
                }

                for (var j = 0; j < invalidChars.Length; j++)
                {
                    if (c == invalidChars[j])
                    {
                        reason = "アセットパスに使用できない文字が含まれています。";
                        return false;
                    }
                }
            }

            reason = null;
            return true;
        }
    }
}
