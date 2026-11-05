using System;
using System.Collections.Generic;
using System.Reflection;
using MasterDataTool.Runtime;
using UnityEngine;

namespace MasterDataTool.EditorTools.Importing
{
    /// <summary>
    /// レコード型のシリアライズ対象フィールド一覧です。
    /// </summary>
    public sealed class SerializedFieldMap
    {
        static readonly Dictionary<Type, SerializedFieldMap> Cache = new Dictionary<Type, SerializedFieldMap>();

        readonly List<SerializedFieldEntry> fields;
        readonly List<string> duplicateFieldNames;

        SerializedFieldMap(Type recordType, List<SerializedFieldEntry> fields, List<string> duplicateFieldNames)
        {
            RecordType = recordType;
            this.fields = fields;
            this.duplicateFieldNames = duplicateFieldNames;
        }

        /// <summary>対象レコード型です。</summary>
        public Type RecordType { get; }

        /// <summary>基底から派生へ並んだシリアライズ対象フィールドです。</summary>
        public IReadOnlyList<SerializedFieldEntry> Fields => fields;

        /// <summary>継承チェーン内で重複したフィールド名です。</summary>
        public IReadOnlyList<string> DuplicateFieldNames => duplicateFieldNames;

        /// <summary>レコード型に対応するマップを取得します(キャッシュあり)。</summary>
        /// <typeparam name="TRecord">レコード型です。</typeparam>
        /// <returns>フィールドマップです。</returns>
        public static SerializedFieldMap Get<TRecord>()
            where TRecord : MasterRecordBase
        {
            return Get(typeof(TRecord));
        }

        /// <summary>レコード型に対応するマップを取得します(キャッシュあり)。</summary>
        /// <param name="recordType">レコード型です。</param>
        /// <returns>フィールドマップです。</returns>
        public static SerializedFieldMap Get(Type recordType)
        {
            if (recordType == null)
            {
                throw new ArgumentNullException(nameof(recordType));
            }

            if (!Cache.TryGetValue(recordType, out var map))
            {
                map = Build(recordType);
                Cache[recordType] = map;
            }

            return map;
        }

        internal static void ClearCacheForTests()
        {
            Cache.Clear();
        }

        static SerializedFieldMap Build(Type recordType)
        {
            var chain = CollectInheritanceChain(recordType);
            var fieldsList = new List<SerializedFieldEntry>();
            var nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < chain.Count; i++)
            {
                var declaredFields = chain[i].GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                for (var f = 0; f < declaredFields.Length; f++)
                {
                    var field = declaredFields[f];
                    if (!IsSerializedField(field))
                    {
                        continue;
                    }

                    fieldsList.Add(new SerializedFieldEntry(field.Name, field));

                    if (nameCounts.TryGetValue(field.Name, out var count))
                    {
                        nameCounts[field.Name] = count + 1;
                    }
                    else
                    {
                        nameCounts[field.Name] = 1;
                    }
                }
            }

            var duplicates = new List<string>();
            foreach (var pair in nameCounts)
            {
                if (pair.Value > 1)
                {
                    duplicates.Add(pair.Key);
                }
            }

            duplicates.Sort(StringComparer.Ordinal);
            return new SerializedFieldMap(recordType, fieldsList, duplicates);
        }

        static List<Type> CollectInheritanceChain(Type recordType)
        {
            var types = new List<Type>();
            for (var current = recordType; current != null && current != typeof(MasterRecordBase); current = current.BaseType)
            {
                types.Add(current);
            }

            types.Reverse();
            return types;
        }

        static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsLiteral)
            {
                return false;
            }

            if (field.Name.IndexOf('<') >= 0)
            {
                return false;
            }

            if (field.IsInitOnly)
            {
                return false;
            }

            if (field.IsDefined(typeof(NonSerializedAttribute), inherit: true))
            {
                return false;
            }

            if (field.IsDefined(typeof(SerializeField), inherit: true))
            {
                return true;
            }

            return field.IsPublic;
        }
    }

    /// <summary>
    /// シリアライズ対象の 1 フィールドです。
    /// </summary>
    public sealed class SerializedFieldEntry
    {
        internal SerializedFieldEntry(string name, FieldInfo fieldInfo)
        {
            Name = name;
            FieldInfo = fieldInfo;
        }

        /// <summary>JSON キー名です。</summary>
        public string Name { get; }

        /// <summary>フィールド情報です。</summary>
        public FieldInfo FieldInfo { get; }
    }
}
