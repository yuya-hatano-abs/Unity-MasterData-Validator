using System;
using System.Collections.Generic;
using System.Reflection;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// レコード型から [MasterRef] 付きフィールドを収集し、コンテキストに依存しない定義を検査します。
    /// </summary>
    internal static class ReferenceFieldCollector
    {
        /// <summary>
        /// レコード型の参照フィールドを、SerializedFieldMap の順序のあと、非シリアライズの [MasterRef] の順で返します。
        /// </summary>
        /// <param name="recordType">レコード型です。</param>
        /// <returns>収集結果です。</returns>
        public static IReadOnlyList<CollectedReferenceField> Collect(Type recordType)
        {
            if (recordType == null)
            {
                throw new ArgumentNullException(nameof(recordType));
            }

            var map = SerializedFieldMap.Get(recordType);
            var serializedRefs = new HashSet<FieldInfo>();
            var result = new List<CollectedReferenceField>();

            for (var i = 0; i < map.Fields.Count; i++)
            {
                var field = map.Fields[i].FieldInfo;
                var attribute = GetMasterRef(field);
                if (attribute == null)
                {
                    continue;
                }

                serializedRefs.Add(field);
                result.Add(Build(field, attribute, isSerialized: true));
            }

            for (var current = recordType; current != null && current != typeof(MasterRecordBase); current = current.BaseType)
            {
                var declared = current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var f = 0; f < declared.Length; f++)
                {
                    var field = declared[f];
                    var attribute = GetMasterRef(field);
                    if (attribute == null || serializedRefs.Contains(field))
                    {
                        continue;
                    }

                    result.Add(Build(field, attribute, isSerialized: false));
                }
            }

            return result;
        }

        static MasterRefAttribute GetMasterRef(FieldInfo field)
        {
            var attributes = field.GetCustomAttributes(typeof(MasterRefAttribute), inherit: false);
            return attributes.Length > 0 ? (MasterRefAttribute)attributes[0] : null;
        }

        static CollectedReferenceField Build(FieldInfo field, MasterRefAttribute attribute, bool isSerialized)
        {
            var collected = new CollectedReferenceField(field, attribute);

            if (!isSerialized)
            {
                collected.DefinitionError =
                    "は [MasterRef] が付いていますが、シリアライズ対象ではありません。";
                return collected;
            }

            if (!TryClassify(field.FieldType, out var isCollection, out var isIntKind))
            {
                collected.DefinitionError =
                    "の型 '" + field.FieldType.Name +
                    "' は、MasterRef が対応する型(int、string、List<int>、List<string>、int[]、string[])ではありません。";
                return collected;
            }

            collected.IsCollection = isCollection;
            collected.IsIntKind = isIntKind;

            var target = attribute.TargetMasterType;
            if (target == null)
            {
                collected.DefinitionError = "の MasterRef の参照先型が null です。";
                return collected;
            }

            if (!typeof(MasterDataBase).IsAssignableFrom(target))
            {
                collected.DefinitionError =
                    "の参照先型 '" + target.Name + "' は MasterDataBase を継承していません。";
                return collected;
            }

            if (target.IsAbstract)
            {
                collected.DefinitionError =
                    "の参照先型 '" + target.Name + "' は abstract です。";
                return collected;
            }

            if (!isIntKind && attribute.HasNoneValue)
            {
                collected.DefinitionError =
                    "は string 系のフィールドですが、NoneValue が指定されています。NoneValue は int 系のフィールドにだけ指定できます。";
                return collected;
            }

            return collected;
        }

        static bool TryClassify(Type fieldType, out bool isCollection, out bool isIntKind)
        {
            isCollection = false;
            isIntKind = false;

            if (fieldType == typeof(int))
            {
                isIntKind = true;
                return true;
            }

            if (fieldType == typeof(string))
            {
                return true;
            }

            if (fieldType.IsArray)
            {
                var element = fieldType.GetElementType();
                if (element == typeof(int))
                {
                    isCollection = true;
                    isIntKind = true;
                    return true;
                }

                if (element == typeof(string))
                {
                    isCollection = true;
                    return true;
                }

                return false;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var element = fieldType.GetGenericArguments()[0];
                if (element == typeof(int))
                {
                    isCollection = true;
                    isIntKind = true;
                    return true;
                }

                if (element == typeof(string))
                {
                    isCollection = true;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 収集した 1 つの参照フィールドです。
    /// </summary>
    internal sealed class CollectedReferenceField
    {
        /// <summary>
        /// 参照フィールドの収集結果を初期化します。
        /// </summary>
        /// <param name="field">フィールドです。</param>
        /// <param name="attribute">[MasterRef] です。</param>
        public CollectedReferenceField(FieldInfo field, MasterRefAttribute attribute)
        {
            Field = field;
            Attribute = attribute;
        }

        /// <summary>フィールドです。</summary>
        public FieldInfo Field { get; }

        /// <summary>[MasterRef] です。</summary>
        public MasterRefAttribute Attribute { get; }

        /// <summary>コレクション(List または配列)の場合に true です。</summary>
        public bool IsCollection { get; set; }

        /// <summary>int 系の場合に true、string 系の場合に false です。</summary>
        public bool IsIntKind { get; set; }

        /// <summary>
        /// コンテキスト非依存の定義エラーの、フィールド名の後に続く文言です。問題がなければ null です。
        /// </summary>
        public string DefinitionError { get; set; }
    }
}
