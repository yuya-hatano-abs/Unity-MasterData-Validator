using System;
using System.Collections.Generic;
using System.Reflection;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// レコード型から [MasterRequired] 付きフィールドを収集し、定義を検査します。
    /// </summary>
    internal static class RequiredFieldCollector
    {
        /// <summary>
        /// レコード型の必須フィールドを、SerializedFieldMap の順序のあと、非シリアライズの [MasterRequired] の順で返します。
        /// </summary>
        /// <param name="recordType">レコード型です。</param>
        /// <returns>収集結果です。</returns>
        public static IReadOnlyList<CollectedRequiredField> Collect(Type recordType)
        {
            if (recordType == null)
            {
                throw new ArgumentNullException(nameof(recordType));
            }

            var map = SerializedFieldMap.Get(recordType);
            var serializedRequired = new HashSet<FieldInfo>();
            var result = new List<CollectedRequiredField>();

            for (var i = 0; i < map.Fields.Count; i++)
            {
                var field = map.Fields[i].FieldInfo;
                if (!HasMasterRequired(field))
                {
                    continue;
                }

                serializedRequired.Add(field);
                result.Add(Build(field, isSerialized: true));
            }

            for (var current = recordType; current != null && current != typeof(MasterRecordBase); current = current.BaseType)
            {
                var declared = current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var f = 0; f < declared.Length; f++)
                {
                    var field = declared[f];
                    if (!HasMasterRequired(field) || serializedRequired.Contains(field))
                    {
                        continue;
                    }

                    result.Add(Build(field, isSerialized: false));
                }
            }

            return result;
        }

        static bool HasMasterRequired(FieldInfo field)
        {
            return field.GetCustomAttributes(typeof(MasterRequiredAttribute), inherit: false).Length > 0;
        }

        static CollectedRequiredField Build(FieldInfo field, bool isSerialized)
        {
            var collected = new CollectedRequiredField(field);

            if (!isSerialized)
            {
                collected.DefinitionError =
                    "は [MasterRequired] が付いていますが、シリアライズ対象ではありません。";
                return collected;
            }

            if (!IsSupportedType(field.FieldType))
            {
                collected.DefinitionError =
                    "の型 '" + field.FieldType.Name +
                    "' は MasterRequired に対応していません(string、List<T>、配列のみ)。";
            }

            return collected;
        }

        static bool IsSupportedType(Type fieldType)
        {
            if (fieldType == typeof(string))
            {
                return true;
            }

            if (fieldType.IsArray)
            {
                return true;
            }

            return fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
        }
    }

    /// <summary>
    /// 収集した 1 つの必須フィールドです。
    /// </summary>
    internal sealed class CollectedRequiredField
    {
        /// <summary>
        /// 必須フィールドの収集結果を初期化します。
        /// </summary>
        /// <param name="field">フィールドです。</param>
        public CollectedRequiredField(FieldInfo field)
        {
            Field = field;
        }

        /// <summary>フィールドです。</summary>
        public FieldInfo Field { get; }

        /// <summary>
        /// 定義エラーの、フィールド名の後に続く文言です。問題がなければ null です。
        /// </summary>
        public string DefinitionError { get; set; }
    }
}
