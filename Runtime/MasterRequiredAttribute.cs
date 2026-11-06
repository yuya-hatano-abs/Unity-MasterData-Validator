using System;
using System.Diagnostics;

namespace MasterDataTool.Runtime
{
    /// <summary>
    /// レコードのフィールドが必須であることを表します。
    /// </summary>
    /// <remarks>
    /// 対応するフィールドの型は、string、List&lt;T&gt;、T[](T は任意の型)です。
    /// 「空」の定義: string は null、空文字、空白だけの文字列です。List と配列は null または要素が 0 件です。
    /// コレクションの要素の中身は検査しません。
    /// int、bool、float、enum などの値型や、UnityEngine.Object 系のフィールドには付けられません
    /// (設定のミスとして報告されます)。
    /// 文字列 ID(<see cref="StringIdMasterRecord"/>)は、この属性がなくても自動的に必須として検査されます。
    /// <see cref="MasterRefAttribute"/> と併用できます。空なら必須のエラー、値があって参照先にないなら
    /// 参照切れのエラーが、それぞれ報告されます。
    /// 使用例:
    /// <code>
    /// [SerializeField, MasterRequired] private string name;
    /// [SerializeField, MasterRequired] private List&lt;int&gt; dropItemIds;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class MasterRequiredAttribute : Attribute
    {
    }
}
