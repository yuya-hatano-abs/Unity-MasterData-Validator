using System;
using System.Diagnostics;

namespace MasterDataTool.Runtime
{
    /// <summary>
    /// レコードのフィールドが、他のマスタのレコードを ID で参照することを表します。
    /// </summary>
    /// <remarks>
    /// 対応するフィールドの型は、int、string、List&lt;int&gt;、List&lt;string&gt;、int[]、string[] です。
    /// int の参照は、既定では 0 を含む全ての値を参照として検査します。「0 は参照なし」のフィールドだけ
    /// <c>NoneValue = 0</c> と明示してください。string の参照は、null と空文字を参照なしとして扱い、検査しません。
    /// <see cref="NoneValue"/> は int のフィールドと、int のコレクションにだけ指定できます。
    /// 参照先のマスタ型は、<see cref="MasterDataBase"/> を継承した abstract でない型です。
    /// 参照先の ID の種類(int / string)は、フィールドの型(int 系 / string 系)と一致している必要があります。
    /// 現時点の制限: 参照先は型で指定します。同じマスタ型のアセットが複数ある場合は未対応です。
    /// 使用例:
    /// <code>
    /// [SerializeField, MasterRef(typeof(ItemMaster))] private int rewardItemId;
    /// [SerializeField, MasterRef(typeof(ItemMaster), NoneValue = 0)] private int optionalItemId;
    /// [SerializeField, MasterRef(typeof(ItemMaster))] private List&lt;int&gt; bonusItemIds;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class MasterRefAttribute : Attribute
    {
        bool hasNoneValue;
        int noneValue;

        /// <summary>
        /// 参照先のマスタ型を指定して属性を初期化します。
        /// </summary>
        /// <param name="targetMasterType">参照先のマスタ型です。</param>
        public MasterRefAttribute(Type targetMasterType)
        {
            TargetMasterType = targetMasterType;
        }

        /// <summary>参照先のマスタ型です。</summary>
        public Type TargetMasterType { get; }

        /// <summary>
        /// <see cref="NoneValue"/> が明示された場合に true です。
        /// </summary>
        public bool HasNoneValue
        {
            get { return hasNoneValue; }
        }

        /// <summary>
        /// 「参照なし」として扱う整数値です。setter が呼ばれると <see cref="HasNoneValue"/> が true になります。
        /// </summary>
        public int NoneValue
        {
            get { return noneValue; }
            set
            {
                noneValue = value;
                hasNoneValue = true;
            }
        }
    }
}
