using System;
using MasterDataTool.Runtime;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// ドロップダウンに出す 1 つのマスタ型です。
    /// </summary>
    internal sealed class MasterTypeInfo
    {
        /// <summary>
        /// マスタ型の情報を初期化します。
        /// </summary>
        /// <param name="masterType">マスタ型です。</param>
        /// <param name="recordType">レコード型です。</param>
        /// <param name="idKind">ID の種類です。</param>
        public MasterTypeInfo(Type masterType, Type recordType, MasterIdKind idKind)
        {
            MasterType = masterType;
            RecordType = recordType;
            IdKind = idKind;
            DisplayName = masterType.FullName;
        }

        /// <summary>マスタ型です。</summary>
        public Type MasterType { get; }

        /// <summary>レコード型です。</summary>
        public Type RecordType { get; }

        /// <summary>ID の種類です。</summary>
        public MasterIdKind IdKind { get; }

        /// <summary>表示名です。マスタ型の FullName です。</summary>
        public string DisplayName { get; }
    }
}
