using System;
using System.Collections.Generic;
using System.IO;
using MasterDataTool.EditorTools.Importing;
using UnityEditor;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// インポーターの入力状態と、入力の検査です。UI Toolkit には依存しません。
    /// </summary>
    internal sealed class MasterToolModel
    {
        readonly Func<string, bool> fileExists;
        readonly Func<string, Type> assetTypeLookup;

        /// <summary>
        /// モデルを初期化します。
        /// </summary>
        /// <param name="state">ウィンドウの状態です。</param>
        /// <param name="catalog">マスタ型の一覧です。</param>
        /// <param name="fileExists">ファイルの存在確認です。null の場合は <see cref="File.Exists"/> です。</param>
        /// <param name="assetTypeLookup">保存先のアセット型です。null の場合は AssetDatabase を使います。</param>
        public MasterToolModel(
            MasterToolState state,
            MasterTypeCatalog catalog,
            Func<string, bool> fileExists = null,
            Func<string, Type> assetTypeLookup = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            State = state;
            Catalog = catalog;
            this.fileExists = fileExists ?? File.Exists;
            this.assetTypeLookup = assetTypeLookup ?? DefaultAssetTypeLookup;
        }

        /// <summary>ウィンドウの状態です。</summary>
        public MasterToolState State { get; }

        /// <summary>マスタ型の一覧です。</summary>
        public MasterTypeCatalog Catalog { get; }

        /// <summary>選択中のマスタ型です。未選択または見つからない場合は null です。</summary>
        public MasterTypeInfo SelectedType
        {
            get
            {
                if (!Catalog.TryFind(Normalize(State.SelectedTypeName), out var info))
                {
                    return null;
                }

                return info;
            }
        }

        /// <summary>
        /// マスタ型を選択します。保存先が空のときだけ、提案パスを入れます。
        /// </summary>
        /// <param name="info">選択する型です。null は未選択です。</param>
        public void SelectType(MasterTypeInfo info)
        {
            if (info == null)
            {
                State.SelectedTypeName = string.Empty;
                return;
            }

            State.SelectedTypeName = info.DisplayName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(State.AssetPath))
            {
                State.AssetPath = SuggestAssetPath(info);
            }
        }

        /// <summary>
        /// JSON パスを設定します。null は空文字になります。
        /// </summary>
        /// <param name="path">JSON パスです。</param>
        public void SetJsonPath(string path)
        {
            State.JsonPath = path ?? string.Empty;
        }

        /// <summary>
        /// 保存先パスを設定します。null は空文字になります。
        /// </summary>
        /// <param name="path">アセットパスです。</param>
        public void SetAssetPath(string path)
        {
            State.AssetPath = path ?? string.Empty;
        }

        /// <summary>
        /// マスタ型から保存先パスの提案値を作ります。
        /// </summary>
        /// <param name="info">マスタ型です。</param>
        /// <returns>Assets/MasterData/&lt;型名&gt;.asset です。</returns>
        public static string SuggestAssetPath(MasterTypeInfo info)
        {
            return "Assets/MasterData/" + info.MasterType.Name + ".asset";
        }

        /// <summary>
        /// 現在の入力を検査します。
        /// </summary>
        /// <returns>問題の一覧です。マスタ型、JSON、保存先の順です。</returns>
        public IReadOnlyList<InputProblem> Validate()
        {
            var problems = new List<InputProblem>();
            ValidateMasterType(problems);
            ValidateJsonPath(problems);
            ValidateAssetPath(problems);
            return problems;
        }

        /// <summary>Error が 1 件もなければ true です。</summary>
        public bool CanProceed
        {
            get
            {
                var problems = Validate();
                for (var i = 0; i < problems.Count; i++)
                {
                    if (problems[i].Severity == InputProblemSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        void ValidateMasterType(List<InputProblem> problems)
        {
            var name = Normalize(State.SelectedTypeName);
            if (string.IsNullOrWhiteSpace(name))
            {
                problems.Add(new InputProblem(
                    InputField.MasterType,
                    InputProblemSeverity.Error,
                    "対象のマスタ型を選択してください。"));
                return;
            }

            if (!Catalog.TryFind(name, out _))
            {
                problems.Add(new InputProblem(
                    InputField.MasterType,
                    InputProblemSeverity.Error,
                    "選択中のマスタ型 '" + name + "' が見つかりません。コードの変更後は、「再読み込み」を押してください。"));
            }
        }

        void ValidateJsonPath(List<InputProblem> problems)
        {
            var path = Normalize(State.JsonPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                problems.Add(new InputProblem(
                    InputField.JsonPath,
                    InputProblemSeverity.Error,
                    "JSON ファイルを指定してください。"));
                return;
            }

            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add(new InputProblem(
                    InputField.JsonPath,
                    InputProblemSeverity.Error,
                    "JSON ファイル(.json)を指定してください。"));
                return;
            }

            if (!fileExists(path))
            {
                problems.Add(new InputProblem(
                    InputField.JsonPath,
                    InputProblemSeverity.Error,
                    "JSON ファイルが見つかりません: " + path));
            }
        }

        void ValidateAssetPath(List<InputProblem> problems)
        {
            var path = Normalize(State.AssetPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                problems.Add(new InputProblem(
                    InputField.AssetPath,
                    InputProblemSeverity.Error,
                    "保存先のアセットパスを指定してください。"));
                return;
            }

            if (!AssetPathValidator.TryValidate(path, out var reason))
            {
                problems.Add(new InputProblem(
                    InputField.AssetPath,
                    InputProblemSeverity.Error,
                    reason));
                return;
            }

            var selected = SelectedType;
            if (selected == null)
            {
                return;
            }

            var existingType = assetTypeLookup(path);
            if (existingType == null)
            {
                return;
            }

            if (!selected.MasterType.IsAssignableFrom(existingType))
            {
                problems.Add(new InputProblem(
                    InputField.AssetPath,
                    InputProblemSeverity.Error,
                    "保存先に、別の型のアセット('" + existingType.Name + "')が既に存在します。"));
                return;
            }

            problems.Add(new InputProblem(
                InputField.AssetPath,
                InputProblemSeverity.Info,
                "既存のアセットを更新します。"));
        }

        static string Normalize(string value)
        {
            return value ?? string.Empty;
        }

        static Type DefaultAssetTypeLookup(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.GetMainAssetTypeAtPath(path);
        }
    }
}
