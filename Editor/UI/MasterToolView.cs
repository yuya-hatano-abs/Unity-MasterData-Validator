using System;
using System.Collections.Generic;
using System.IO;
using MasterDataTool.EditorTools.Pipeline;
using MasterDataTool.EditorTools.Validation;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// インポーターウィンドウの UI Toolkit ビューです。
    /// </summary>
    internal sealed class MasterToolView
    {
        /// <summary>対象マスタのドロップダウン名です。</summary>
        internal const string MasterTypeFieldName = "master-type-field";

        /// <summary>マスタ型の再読み込みボタン名です。</summary>
        internal const string MasterTypeRefreshButtonName = "master-type-refresh-button";

        /// <summary>マスタ型が 0 件のときの案内名です。</summary>
        internal const string MasterTypeEmptyHelpName = "master-type-empty-help";

        /// <summary>JSON パス欄の名前です。</summary>
        internal const string JsonPathFieldName = "json-path-field";

        /// <summary>JSON 参照ボタンの名前です。</summary>
        internal const string JsonBrowseButtonName = "json-browse-button";

        /// <summary>保存先パス欄の名前です。</summary>
        internal const string AssetPathFieldName = "asset-path-field";

        /// <summary>保存先参照ボタンの名前です。</summary>
        internal const string AssetBrowseButtonName = "asset-browse-button";

        /// <summary>入力の問題の表示領域の名前です。</summary>
        internal const string InputProblemsName = "input-problems";

        /// <summary>検証ボタンの名前です。</summary>
        internal const string ValidateButtonName = "validate-button";

        /// <summary>検証して保存ボタンの名前です。</summary>
        internal const string SaveButtonName = "save-button";

        /// <summary>検証結果領域の名前です。</summary>
        internal const string ResultsContainerName = "results-container";

        /// <summary>検証結果の見出し行を入れる要素の名前です。</summary>
        internal const string ResultsHeaderName = "results-header";

        /// <summary>検証問題の一覧パネルの名前です。</summary>
        internal const string ResultsListPanelName = "results-list-panel";

        const string ResultHeading = "検証結果";
        const string ResultPlaceholder = "検証結果は、ここに表示されます。";
        const string TooltipNeedInput = "入力の問題を解消してください。";
        const string TooltipRunning = "実行中です。";
        const string TooltipValidate = "JSON を検証します(保存しません)。";
        const string TooltipSave = "検証し、エラーがなければ保存します。";

        readonly MasterToolModel model;
        readonly IFileDialogs fileDialogs;
        readonly IImportRunner importRunner;
        readonly ValidationResultsView resultsView;

        DropdownField masterTypeField;
        HelpBox emptyTypeHelp;
        TextField jsonPathField;
        TextField assetPathField;
        VisualElement inputProblems;
        Button validateButton;
        Button saveButton;
        VisualElement resultsContainer;
        bool refreshing;
        bool isRunning;

        /// <summary>
        /// ビューを初期化します。
        /// </summary>
        /// <param name="model">入力モデルです。</param>
        /// <param name="fileDialogs">ファイル選択です。null の場合はエディタのダイアログです。</param>
        /// <param name="importRunner">インポートの実行です。null の場合はプロジェクトを走査する実装です。</param>
        /// <param name="assetSelector">結果の行からアセットを選ぶ実装です。null の場合はエディタの選択です。</param>
        public MasterToolView(
            MasterToolModel model,
            IFileDialogs fileDialogs = null,
            IImportRunner importRunner = null,
            IAssetSelector assetSelector = null)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
            this.fileDialogs = fileDialogs ?? new EditorFileDialogs();
            this.importRunner = importRunner ?? new ProjectImportRunner();
            resultsView = new ValidationResultsView(null, assetSelector);
        }

        /// <summary>検証結果の一覧です。行の選択をテストから再現します。</summary>
        internal ValidationResultsView ResultsView => resultsView;

        /// <summary>
        /// 要素ツリーを作り、現在の状態を反映します。
        /// </summary>
        /// <returns>ウィンドウへ追加するルート要素です。</returns>
        public VisualElement Build()
        {
            var root = new ScrollView();

            var title = new Label(MasterDataTool.EditorTools.MasterDataToolInfo.ToolName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;
            title.style.marginBottom = 8;
            root.Add(title);

            root.Add(new Label("対象マスタ"));
            var typeRow = CreateRow();
            masterTypeField = new DropdownField { name = MasterTypeFieldName };
            masterTypeField.style.flexGrow = 1;
            masterTypeField.RegisterValueChangedCallback(evt => OnMasterTypeChanged(evt.newValue));
            typeRow.Add(masterTypeField);
            var refreshButton = new Button(OnRefreshClicked) { name = MasterTypeRefreshButtonName, text = "再読み込み" };
            typeRow.Add(refreshButton);
            root.Add(typeRow);

            emptyTypeHelp = new HelpBox(
                "マスタ型(MasterDataBase<T> を継承した、abstract でないクラス)が見つかりません。",
                HelpBoxMessageType.Info)
            {
                name = MasterTypeEmptyHelpName,
            };
            root.Add(emptyTypeHelp);

            root.Add(new Label("JSON ファイル"));
            var jsonRow = CreateRow();
            jsonPathField = new TextField { name = JsonPathFieldName };
            jsonPathField.style.flexGrow = 1;
            jsonPathField.RegisterValueChangedCallback(evt => OnJsonPathChanged(evt.newValue));
            jsonRow.Add(jsonPathField);
            jsonRow.Add(new Button(OnBrowseJsonClicked) { name = JsonBrowseButtonName, text = "参照..." });
            root.Add(jsonRow);

            root.Add(new Label("保存先アセット"));
            var assetRow = CreateRow();
            assetPathField = new TextField { name = AssetPathFieldName };
            assetPathField.style.flexGrow = 1;
            assetPathField.RegisterValueChangedCallback(evt => OnAssetPathChanged(evt.newValue));
            assetRow.Add(assetPathField);
            assetRow.Add(new Button(OnBrowseAssetClicked) { name = AssetBrowseButtonName, text = "参照..." });
            root.Add(assetRow);

            inputProblems = new VisualElement { name = InputProblemsName };
            inputProblems.style.marginTop = 8;
            root.Add(inputProblems);

            var buttonRow = CreateRow();
            validateButton = new Button(OnValidateClicked) { name = ValidateButtonName, text = "検証" };
            saveButton = new Button(OnSaveClicked) { name = SaveButtonName, text = "検証して保存" };
            buttonRow.Add(validateButton);
            buttonRow.Add(saveButton);
            root.Add(buttonRow);

            resultsContainer = new VisualElement { name = ResultsContainerName };
            resultsContainer.style.marginTop = 12;
            root.Add(resultsContainer);
            ShowPlaceholder();

            Refresh();
            return root;
        }

        /// <summary>
        /// モデルの状態を画面へ反映します。入力欄の変更通知は出しません。結果の行は変えません。
        /// </summary>
        public void Refresh()
        {
            refreshing = true;
            try
            {
                var choices = new List<string>();
                var types = model.Catalog.Types;
                for (var i = 0; i < types.Count; i++)
                {
                    choices.Add(types[i].DisplayName);
                }

                masterTypeField.choices = choices;
                var selectedName = model.State.SelectedTypeName ?? string.Empty;
                if (!string.IsNullOrEmpty(selectedName) && choices.Contains(selectedName))
                {
                    masterTypeField.SetValueWithoutNotify(selectedName);
                }
                else
                {
                    masterTypeField.index = -1;
                }

                emptyTypeHelp.style.display = types.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

                jsonPathField.SetValueWithoutNotify(model.State.JsonPath ?? string.Empty);
                assetPathField.SetValueWithoutNotify(model.State.AssetPath ?? string.Empty);

                inputProblems.Clear();
                var problems = model.Validate();
                for (var i = 0; i < problems.Count; i++)
                {
                    var messageType = problems[i].Severity == InputProblemSeverity.Error
                        ? HelpBoxMessageType.Error
                        : HelpBoxMessageType.Info;
                    inputProblems.Add(new HelpBox(problems[i].Message, messageType));
                }

                ApplyExecutionButtons();
            }
            finally
            {
                refreshing = false;
            }
        }

        /// <summary>
        /// ドロップダウンの選択が変わったときに呼ばれます。
        /// </summary>
        /// <param name="displayName">選択された表示名です。</param>
        internal void OnMasterTypeChanged(string displayName)
        {
            if (refreshing)
            {
                return;
            }

            DiscardResults();
            if (model.Catalog.TryFind(displayName, out var info))
            {
                model.SelectType(info);
            }
            else
            {
                model.SelectType(null);
            }

            Refresh();
        }

        /// <summary>
        /// 再読み込みボタンが押されたときに呼ばれます。結果の表示は残します。
        /// </summary>
        internal void OnRefreshClicked()
        {
            if (refreshing)
            {
                return;
            }

            model.Catalog.Refresh();
            Refresh();
        }

        /// <summary>
        /// JSON パスが編集されたときに呼ばれます。
        /// </summary>
        /// <param name="value">新しいパスです。</param>
        internal void OnJsonPathChanged(string value)
        {
            if (refreshing)
            {
                return;
            }

            DiscardResults();
            model.SetJsonPath(value);
            Refresh();
        }

        /// <summary>
        /// 保存先パスが編集されたときに呼ばれます。
        /// </summary>
        /// <param name="value">新しいパスです。</param>
        internal void OnAssetPathChanged(string value)
        {
            if (refreshing)
            {
                return;
            }

            DiscardResults();
            model.SetAssetPath(value);
            Refresh();
        }

        /// <summary>
        /// JSON の参照ボタンが押されたときに呼ばれます。
        /// </summary>
        internal void OnBrowseJsonClicked()
        {
            if (refreshing)
            {
                return;
            }

            var selected = fileDialogs.OpenJsonFile(ResolveInitialJsonDirectory());
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            DiscardResults();
            model.SetJsonPath(selected);
            var directory = Path.GetDirectoryName(selected);
            model.State.LastJsonDirectory = directory ?? string.Empty;
            Refresh();
        }

        /// <summary>
        /// 保存先の参照ボタンが押されたときに呼ばれます。
        /// </summary>
        internal void OnBrowseAssetClicked()
        {
            if (refreshing)
            {
                return;
            }

            var selected = model.SelectedType;
            var suggestedName = selected == null ? "Master.asset" : selected.MasterType.Name + ".asset";
            var picked = fileDialogs.SaveAssetFile(suggestedName, ResolveInitialAssetFolder());
            if (string.IsNullOrEmpty(picked))
            {
                return;
            }

            DiscardResults();
            model.SetAssetPath(picked);
            Refresh();
        }

        /// <summary>
        /// 検証ボタンが押されたときに呼ばれます。保存はしません。
        /// </summary>
        internal void OnValidateClicked()
        {
            if (isRunning)
            {
                return;
            }

            RunPipeline(PipelineMode.Validate);
        }

        /// <summary>
        /// 検証して保存ボタンが押されたときに呼ばれます。
        /// </summary>
        internal void OnSaveClicked()
        {
            if (isRunning)
            {
                return;
            }

            RunPipeline(PipelineMode.ValidateAndSave);
        }

        void RunPipeline(PipelineMode mode)
        {
            if (isRunning || !model.CanProceed)
            {
                return;
            }

            var selected = model.SelectedType;
            if (selected == null)
            {
                return;
            }

            isRunning = true;
            Refresh();
            PipelineResult result = null;
            IReadOnlyList<ResultLine> exceptionLines = null;
            try
            {
                var request = new PipelineRequest(
                    selected.MasterType,
                    selected.RecordType,
                    model.State.JsonPath ?? string.Empty,
                    model.State.AssetPath ?? string.Empty,
                    mode);
                result = importRunner.Run(request);
            }
            catch (Exception ex)
            {
                exceptionLines = new[]
                {
                    new ResultLine(
                        ResultLineSeverity.Error,
                        "実行できませんでした: " + ex.GetType().Name + ": " + ex.Message),
                };
            }
            finally
            {
                isRunning = false;
                Refresh();
            }

            if (exceptionLines != null)
            {
                ShowExecutedResult(exceptionLines, null, false);
                return;
            }

            var showList = result.Status != PipelineStatus.InvalidRequest &&
                           result.Status != PipelineStatus.UnexpectedError;
            IReadOnlyList<ValidationIssue> issues = null;
            if (showList && result.Validation != null)
            {
                issues = result.Validation.Issues;
            }

            ShowExecutedResult(PipelineResultFormatter.FormatHeader(result), issues, showList);
        }

        void ApplyExecutionButtons()
        {
            var enabled = model.CanProceed && !isRunning;
            validateButton.SetEnabled(enabled);
            saveButton.SetEnabled(enabled);
            if (isRunning)
            {
                validateButton.tooltip = TooltipRunning;
                saveButton.tooltip = TooltipRunning;
            }
            else if (!model.CanProceed)
            {
                validateButton.tooltip = TooltipNeedInput;
                saveButton.tooltip = TooltipNeedInput;
            }
            else
            {
                validateButton.tooltip = TooltipValidate;
                saveButton.tooltip = TooltipSave;
            }
        }

        void DiscardResults()
        {
            resultsView.Clear();
            ShowPlaceholder();
        }

        void ShowPlaceholder()
        {
            if (resultsContainer == null)
            {
                return;
            }

            resultsContainer.Clear();
            resultsContainer.Add(new Label(ResultHeading));
            resultsContainer.Add(new Label(ResultPlaceholder));
        }

        void ShowExecutedResult(
            IReadOnlyList<ResultLine> lines,
            IReadOnlyList<ValidationIssue> issues,
            bool showList)
        {
            if (resultsContainer == null)
            {
                return;
            }

            resultsContainer.Clear();
            resultsContainer.Add(new Label(ResultHeading));
            var header = new VisualElement { name = ResultsHeaderName };
            if (lines != null)
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    header.Add(new Label(lines[i].Text));
                }
            }

            resultsContainer.Add(header);
            if (!showList)
            {
                resultsView.Clear();
                return;
            }

            resultsContainer.Add(resultsView.Build());
            resultsView.SetIssues(issues);
        }

        string ResolveInitialJsonDirectory()
        {
            if (!string.IsNullOrWhiteSpace(model.State.LastJsonDirectory))
            {
                return model.State.LastJsonDirectory;
            }

            if (!string.IsNullOrWhiteSpace(model.State.JsonPath))
            {
                var directory = Path.GetDirectoryName(model.State.JsonPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    return directory;
                }
            }

            var parent = Directory.GetParent(Application.dataPath);
            return parent == null ? Application.dataPath : parent.FullName;
        }

        string ResolveInitialAssetFolder()
        {
            var path = model.State.AssetPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Assets";
            }

            var folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
            {
                return "Assets";
            }

            folder = folder.Replace('\\', '/');
            if (folder.StartsWith("Assets", StringComparison.Ordinal))
            {
                return folder;
            }

            return "Assets";
        }

        static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;
            return row;
        }
    }
}
