using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;
using UnityEngine.UIElements;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// 検証問題のフィルターと一覧です。
    /// </summary>
    internal sealed class ValidationResultsView
    {
        /// <summary>パネルの要素名です。</summary>
        internal const string PanelName = "results-list-panel";

        /// <summary>フィルターバーの要素名です。</summary>
        internal const string FilterBarName = "results-filter-bar";

        /// <summary>エラーのトグル名です。</summary>
        internal const string ErrorsToggleName = "results-filter-errors";

        /// <summary>警告のトグル名です。</summary>
        internal const string WarningsToggleName = "results-filter-warnings";

        /// <summary>種類ドロップダウンの要素名です。</summary>
        internal const string KindFieldName = "results-filter-kind";

        /// <summary>表示件数ラベルの要素名です。</summary>
        internal const string VisibleCountName = "results-visible-count";

        /// <summary>一覧の要素名です。</summary>
        internal const string ListName = "results-list";

        /// <summary>空表示ラベルの要素名です。</summary>
        internal const string EmptyLabelName = "results-empty-label";

        /// <summary>選択結果ラベルの要素名です。</summary>
        internal const string SelectionStatusName = "results-selection-status";

        const string AllKindsLabel = "すべて";
        const string NoAssetMessage = "この問題には、対応するアセットがありません。";
        const string MissingAssetPrefix = "アセットが見つかりません(未保存の可能性があります): ";
        const string SelectedPrefix = "選択しました: ";

        readonly ValidationResultsModel model;
        readonly IAssetSelector assetSelector;

        Toggle errorsToggle;
        Toggle warningsToggle;
        DropdownField kindField;
        Label visibleCountLabel;
        ListView listView;
        Label emptyLabel;
        Label selectionStatus;
        bool suppressCallbacks;
        bool suppressSelection;

        /// <summary>
        /// ビューを初期化します。
        /// </summary>
        /// <param name="model">フィルター状態です。null の場合は新しいモデルです。</param>
        /// <param name="assetSelector">行選択時のアセット選択です。null の場合はエディタの選択です。</param>
        public ValidationResultsView(ValidationResultsModel model = null, IAssetSelector assetSelector = null)
        {
            this.model = model ?? new ValidationResultsModel();
            this.assetSelector = assetSelector ?? new EditorAssetSelector();
        }

        /// <summary>フィルター状態です。</summary>
        internal ValidationResultsModel Model => model;

        /// <summary>
        /// 要素ツリーを作り、現在のモデルを反映します。
        /// </summary>
        /// <returns>結果パネルです。</returns>
        public VisualElement Build()
        {
            var root = new VisualElement { name = PanelName };

            var filterBar = new VisualElement { name = FilterBarName };
            filterBar.style.flexDirection = FlexDirection.Row;
            filterBar.style.flexWrap = Wrap.Wrap;
            filterBar.style.alignItems = Align.Center;
            filterBar.style.marginBottom = 6;

            errorsToggle = new Toggle { name = ErrorsToggleName };
            errorsToggle.style.marginRight = 8;
            errorsToggle.RegisterValueChangedCallback(evt => OnShowErrorsChanged(evt.newValue));
            filterBar.Add(errorsToggle);

            warningsToggle = new Toggle { name = WarningsToggleName };
            warningsToggle.style.marginRight = 8;
            warningsToggle.RegisterValueChangedCallback(evt => OnShowWarningsChanged(evt.newValue));
            filterBar.Add(warningsToggle);

            kindField = new DropdownField { name = KindFieldName };
            kindField.style.flexGrow = 1;
            kindField.style.marginRight = 8;
            kindField.RegisterValueChangedCallback(evt => OnKindFilterChanged(evt.newValue));
            filterBar.Add(kindField);

            visibleCountLabel = new Label { name = VisibleCountName };
            filterBar.Add(visibleCountLabel);
            root.Add(filterBar);

            listView = new ListView { name = ListName };
            listView.selectionType = SelectionType.Single;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.fixedItemHeight = 48;
            listView.style.flexGrow = 0;
            listView.style.flexShrink = 0;
            listView.style.height = 240;
            listView.makeItem = MakeItem;
            listView.bindItem = BindItem;
            listView.itemsSource = new List<ValidationIssue>();
            listView.selectionChanged += OnSelectionChanged;
            root.Add(listView);

            emptyLabel = new Label { name = EmptyLabelName };
            emptyLabel.style.marginTop = 4;
            emptyLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(emptyLabel);

            selectionStatus = new Label { name = SelectionStatusName };
            selectionStatus.style.marginTop = 4;
            selectionStatus.style.whiteSpace = WhiteSpace.Normal;
            selectionStatus.text = string.Empty;
            root.Add(selectionStatus);

            Refresh();
            return root;
        }

        /// <summary>
        /// 問題を差し替え、画面を更新し、選択結果の文言を消します。
        /// </summary>
        /// <param name="issues">新しい問題です。null は空です。</param>
        public void SetIssues(IEnumerable<ValidationIssue> issues)
        {
            model.SetIssues(issues);
            Refresh();
            SetSelectionStatus(string.Empty);
        }

        /// <summary>
        /// 問題を空にし、画面を更新し、選択結果の文言を消します。重大度のトグルは残します。
        /// </summary>
        public void Clear()
        {
            model.Clear();
            Refresh();
            SetSelectionStatus(string.Empty);
        }

        /// <summary>
        /// モデルの状態を画面へ反映します。モデルは変えません。選択結果の文言も変えません。
        /// </summary>
        public void Refresh()
        {
            if (listView == null)
            {
                return;
            }

            suppressCallbacks = true;
            suppressSelection = true;
            try
            {
                errorsToggle.SetValueWithoutNotify(model.ShowErrors);
                errorsToggle.text = "エラー (" + model.ErrorCount + ")";
                warningsToggle.SetValueWithoutNotify(model.ShowWarnings);
                warningsToggle.text = "警告 (" + model.WarningCount + ")";

                var choices = new List<string> { AllKindsLabel };
                var kinds = model.AvailableKinds;
                for (var i = 0; i < kinds.Count; i++)
                {
                    choices.Add(ValidationKindLabels.GetLabel(kinds[i]));
                }

                kindField.choices = choices;
                var selected = model.KindFilter == null
                    ? AllKindsLabel
                    : ValidationKindLabels.GetLabel(model.KindFilter.Value);
                if (!choices.Contains(selected))
                {
                    selected = AllKindsLabel;
                }

                kindField.SetValueWithoutNotify(selected);

                var visible = model.VisibleIssues;
                var items = new List<ValidationIssue>(visible.Count);
                for (var i = 0; i < visible.Count; i++)
                {
                    items.Add(visible[i]);
                }

                listView.itemsSource = items;
                visibleCountLabel.text = "表示中: " + items.Count + " / " + model.AllIssues.Count + " 件";
                ApplyEmptyLabel(items.Count);
            }
            finally
            {
                suppressSelection = false;
                suppressCallbacks = false;
            }
        }

        /// <summary>
        /// エラー表示のトグルが変わったときに呼ばれます。
        /// </summary>
        /// <param name="value">エラーを表示するかです。</param>
        internal void OnShowErrorsChanged(bool value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            model.ShowErrors = value;
            Refresh();
            SetSelectionStatus(string.Empty);
        }

        /// <summary>
        /// 警告表示のトグルが変わったときに呼ばれます。
        /// </summary>
        /// <param name="value">警告を表示するかです。</param>
        internal void OnShowWarningsChanged(bool value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            model.ShowWarnings = value;
            Refresh();
            SetSelectionStatus(string.Empty);
        }

        /// <summary>
        /// 種類ドロップダウンが変わったときに呼ばれます。「すべて」と未知の名前は、絞り込みなしです。
        /// </summary>
        /// <param name="label">選択された表示名です。</param>
        internal void OnKindFilterChanged(string label)
        {
            if (suppressCallbacks)
            {
                return;
            }

            model.KindFilter = FindKind(label);
            Refresh();
            SetSelectionStatus(string.Empty);
        }

        /// <summary>
        /// 一覧の行が選ばれたときに呼ばれます。パスが空なら、選択はしません。null は何もしません。
        /// </summary>
        /// <param name="issue">選ばれた問題です。</param>
        internal void OnIssueSelected(ValidationIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(issue.AssetPath))
            {
                SetSelectionStatus(NoAssetMessage);
                return;
            }

            if (assetSelector.TrySelect(issue.AssetPath))
            {
                SetSelectionStatus(SelectedPrefix + issue.AssetPath);
                return;
            }

            SetSelectionStatus(MissingAssetPrefix + issue.AssetPath);
        }

        /// <summary>
        /// 一覧の選択イベントです。最初の <see cref="ValidationIssue"/> だけを渡します。
        /// 空、または問題でない要素だけのときは、選択結果の文言を変えません。
        /// 同じ行の再クリックは selectionChanged を再発火しません。これは Unity の仕様として扱います。
        /// </summary>
        /// <param name="selected">選択された要素です。</param>
        internal void OnSelectionChanged(IEnumerable<object> selected)
        {
            if (suppressSelection || selected == null)
            {
                return;
            }

            foreach (var item in selected)
            {
                var issue = item as ValidationIssue;
                if (issue != null)
                {
                    OnIssueSelected(issue);
                    return;
                }
            }
        }

        VisualElement MakeItem()
        {
            var row = new VisualElement();
            var primary = new Label { name = "issue-primary" };
            primary.style.whiteSpace = WhiteSpace.Normal;
            var detail = new Label { name = "issue-detail" };
            detail.style.whiteSpace = WhiteSpace.Normal;
            detail.style.fontSize = 11;
            detail.style.marginTop = 2;
            row.Add(primary);
            row.Add(detail);
            return row;
        }

        void BindItem(VisualElement element, int index)
        {
            var source = listView.itemsSource;
            if (source == null || index < 0 || index >= source.Count)
            {
                return;
            }

            var issue = source[index] as ValidationIssue;
            if (issue == null)
            {
                return;
            }

            var primary = element.Q<Label>("issue-primary");
            var detail = element.Q<Label>("issue-detail");
            primary.text = IssueRowFormatter.FormatPrimary(issue);
            detail.text = IssueRowFormatter.FormatDetail(issue);
            element.tooltip = IssueRowFormatter.FormatTooltip(issue);
        }

        void ApplyEmptyLabel(int visibleCount)
        {
            if (model.AllIssues.Count == 0)
            {
                emptyLabel.text = "問題はありません。";
                emptyLabel.style.display = DisplayStyle.Flex;
                return;
            }

            if (visibleCount == 0)
            {
                emptyLabel.text = "フィルター条件に一致する問題はありません。";
                emptyLabel.style.display = DisplayStyle.Flex;
                return;
            }

            emptyLabel.style.display = DisplayStyle.None;
        }

        ValidationIssueKind? FindKind(string label)
        {
            if (string.IsNullOrEmpty(label) || label == AllKindsLabel)
            {
                return null;
            }

            var kinds = model.AvailableKinds;
            for (var i = 0; i < kinds.Count; i++)
            {
                if (ValidationKindLabels.GetLabel(kinds[i]) == label)
                {
                    return kinds[i];
                }
            }

            return null;
        }

        void SetSelectionStatus(string text)
        {
            if (selectionStatus == null)
            {
                return;
            }

            selectionStatus.text = text ?? string.Empty;
        }
    }
}
