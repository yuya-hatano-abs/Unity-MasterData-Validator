using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using UnityEditor;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// プロジェクト内のマスタアセットから、参照先テーブルを集めます。
    /// </summary>
    internal sealed class ProjectMasterTableCollector
    {
        /// <summary>プロジェクト警告に付ける検証器 ID です。</summary>
        public const string ValidatorId = "project-tables";

        readonly IMasterAssetSource source;

        /// <summary>
        /// コレクタを初期化します。
        /// </summary>
        /// <param name="source">アセットの読み込み元です。null の場合は AssetDatabase です。</param>
        public ProjectMasterTableCollector(IMasterAssetSource source = null)
        {
            this.source = source ?? new AssetDatabaseMasterAssetSource();
        }

        /// <summary>
        /// 参照先テーブルと警告を集めます。
        /// </summary>
        /// <returns>テーブルと警告です。</returns>
        public ProjectTables Collect()
        {
            var loaded = source.LoadAll();
            var byType = new Dictionary<Type, List<MasterDataBase>>();
            if (loaded != null)
            {
                for (var i = 0; i < loaded.Count; i++)
                {
                    var asset = loaded[i];
                    if (asset == null)
                    {
                        continue;
                    }

                    var type = asset.GetType();
                    List<MasterDataBase> group;
                    if (!byType.TryGetValue(type, out group))
                    {
                        group = new List<MasterDataBase>();
                        byType.Add(type, group);
                    }

                    group.Add(asset);
                }
            }

            var tables = new List<MasterTable>();
            var warnings = new List<ValidationIssue>();
            foreach (var pair in byType)
            {
                var group = pair.Value;
                if (group.Count >= 2)
                {
                    warnings.Add(CreateMultipleAssetsWarning(pair.Key, group));
                    continue;
                }

                tables.Add(MasterTable.FromAsset(group[0]));
            }

            var byName = new Dictionary<string, List<MasterTable>>(StringComparer.Ordinal);
            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                List<MasterTable> named;
                if (!byName.TryGetValue(table.Name, out named))
                {
                    named = new List<MasterTable>();
                    byName.Add(table.Name, named);
                }

                named.Add(table);
            }

            var kept = new List<MasterTable>();
            foreach (var pair in byName)
            {
                if (pair.Value.Count >= 2)
                {
                    warnings.Add(CreateAmbiguousNameWarning(pair.Key, pair.Value));
                    continue;
                }

                kept.Add(pair.Value[0]);
            }

            kept.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            warnings.Sort(CompareWarnings);
            return new ProjectTables(kept, warnings);
        }

        static ValidationIssue CreateMultipleAssetsWarning(Type type, List<MasterDataBase> assets)
        {
            var paths = new List<string>();
            for (var i = 0; i < assets.Count; i++)
            {
                paths.Add(DescribePath(assets[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            var message = "マスタ型 '" + type.Name + "' のアセットが " + assets.Count +
                          " 件あるため、参照先として使えません: " + Join(paths);
            return new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.MultipleMasterAssets,
                ValidatorId,
                message,
                type.Name,
                FirstSavedAssetPath(assets));
        }

        static ValidationIssue CreateAmbiguousNameWarning(string name, List<MasterTable> tables)
        {
            var fullNames = new List<string>();
            for (var i = 0; i < tables.Count; i++)
            {
                var type = tables[i].MasterType;
                fullNames.Add(type == null ? name : type.FullName);
            }

            fullNames.Sort(StringComparer.Ordinal);
            var message = "同じ名前 '" + name + "' のマスタ型が複数あるため(" + Join(fullNames) +
                          ")、参照先として使えません。";
            return new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.AmbiguousMasterName,
                ValidatorId,
                message,
                name,
                FirstTableAssetPath(tables));
        }

        /// <summary>
        /// 保存済みパスのうち、序数順で最初のものです。メッセージは未保存も含めて並べます。
        /// </summary>
        static string FirstSavedAssetPath(List<MasterDataBase> assets)
        {
            var paths = new List<string>();
            for (var i = 0; i < assets.Count; i++)
            {
                var path = AssetDatabase.GetAssetPath(assets[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            if (paths.Count == 0)
            {
                return null;
            }

            paths.Sort(StringComparer.Ordinal);
            return paths[0];
        }

        /// <summary>
        /// 空でない AssetPath のうち、マスタ型の完全名の序数順で最初のものです。型が null のテーブルは後です。
        /// </summary>
        static string FirstTableAssetPath(List<MasterTable> tables)
        {
            MasterTable first = null;
            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                if (table == null || string.IsNullOrEmpty(table.AssetPath))
                {
                    continue;
                }

                if (first == null || CompareByTypeName(table, first) < 0)
                {
                    first = table;
                }
            }

            return first == null ? null : first.AssetPath;
        }

        static int CompareByTypeName(MasterTable left, MasterTable right)
        {
            if (left.MasterType == null && right.MasterType == null)
            {
                return 0;
            }

            if (left.MasterType == null)
            {
                return 1;
            }

            if (right.MasterType == null)
            {
                return -1;
            }

            return string.CompareOrdinal(left.MasterType.FullName, right.MasterType.FullName);
        }

        static string DescribePath(MasterDataBase asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                return "(未保存)";
            }

            return path;
        }

        static string Join(List<string> values)
        {
            var text = string.Empty;
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    text += ", ";
                }

                text += values[i];
            }

            return text;
        }

        static int CompareWarnings(ValidationIssue left, ValidationIssue right)
        {
            var name = string.CompareOrdinal(left.MasterName ?? string.Empty, right.MasterName ?? string.Empty);
            if (name != 0)
            {
                return name;
            }

            return left.Kind.CompareTo(right.Kind);
        }
    }
}
