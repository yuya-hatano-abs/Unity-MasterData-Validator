using System;
using System.Collections.Generic;
using MasterDataTool.Runtime;
using UnityEditor;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// プロジェクト内の具象マスタ型の一覧です。
    /// </summary>
    internal sealed class MasterTypeCatalog
    {
        readonly Func<Type, bool> includeFilter;
        List<MasterTypeInfo> types = new List<MasterTypeInfo>();

        /// <summary>
        /// カタログを初期化し、一覧を構築します。
        /// </summary>
        /// <param name="includeFilter">含める型の条件です。null の場合はテスト用アセンブリを除外します。</param>
        public MasterTypeCatalog(Func<Type, bool> includeFilter = null)
        {
            this.includeFilter = includeFilter ?? DefaultIncludeFilter;
            Refresh();
        }

        /// <summary>表示名の昇順に並んだマスタ型です。</summary>
        public IReadOnlyList<MasterTypeInfo> Types
        {
            get { return types; }
        }

        /// <summary>
        /// 表示名が一致するマスタ型を取得します。
        /// </summary>
        /// <param name="fullName">マスタ型の FullName です。</param>
        /// <param name="info">見つかった情報です。</param>
        /// <returns>見つかった場合は true です。</returns>
        public bool TryFind(string fullName, out MasterTypeInfo info)
        {
            info = null;
            if (string.IsNullOrEmpty(fullName))
            {
                return false;
            }

            for (var i = 0; i < types.Count; i++)
            {
                if (string.Equals(types[i].DisplayName, fullName, StringComparison.Ordinal))
                {
                    info = types[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// TypeCache から一覧を再構築します。
        /// </summary>
        public void Refresh()
        {
            var derived = TypeCache.GetTypesDerivedFrom<MasterDataBase>();
            var list = new List<MasterTypeInfo>();
            for (var i = 0; i < derived.Count; i++)
            {
                var masterType = derived[i];
                if (masterType.IsAbstract || masterType.IsGenericTypeDefinition)
                {
                    continue;
                }

                if (!TryGetRecordType(masterType, out var recordType))
                {
                    continue;
                }

                if (!includeFilter(masterType))
                {
                    continue;
                }

                if (!TryResolveIdKind(recordType, out var idKind))
                {
                    continue;
                }

                list.Add(new MasterTypeInfo(masterType, recordType, idKind));
            }

            list.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            types = list;
        }

        static bool DefaultIncludeFilter(Type masterType)
        {
            var assemblyName = masterType.Assembly.GetName().Name;
            return assemblyName == null || !assemblyName.EndsWith(".Tests", StringComparison.Ordinal);
        }

        static bool TryGetRecordType(Type masterType, out Type recordType)
        {
            recordType = null;
            for (var current = masterType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                {
                    continue;
                }

                if (current.GetGenericTypeDefinition() != typeof(MasterDataBase<>))
                {
                    continue;
                }

                recordType = current.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        static bool TryResolveIdKind(Type recordType, out MasterIdKind idKind)
        {
            if (typeof(IntIdMasterRecord).IsAssignableFrom(recordType))
            {
                idKind = MasterIdKind.Int;
                return true;
            }

            if (typeof(StringIdMasterRecord).IsAssignableFrom(recordType))
            {
                idKind = MasterIdKind.String;
                return true;
            }

            idKind = default(MasterIdKind);
            return false;
        }
    }
}
