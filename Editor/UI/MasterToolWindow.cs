using MasterDataTool.EditorTools.Logging;
using MasterDataTool.EditorTools.Pipeline;
using UnityEditor;
using UnityEngine;

namespace MasterDataTool.EditorTools.UI
{
    /// <summary>
    /// Master Data Tool のインポーターウィンドウです。
    /// </summary>
    public sealed class MasterToolWindow : EditorWindow
    {
        [SerializeField]
        MasterToolState state = new MasterToolState();

        /// <summary>
        /// メニューからウィンドウを開きます。
        /// </summary>
        [MenuItem(MasterDataTool.EditorTools.MasterDataToolInfo.MenuRoot + "/Importer")]
        public static void Open()
        {
            var window = GetWindow<MasterToolWindow>();
            window.titleContent = new GUIContent(MasterDataTool.EditorTools.MasterDataToolInfo.ToolName);
            window.minSize = new Vector2(420, 360);
        }

        /// <summary>
        /// UI Toolkit の画面を組み立てます。Unity がウィンドウ表示時に呼び出します。
        /// </summary>
        public void CreateGUI()
        {
            if (state == null)
            {
                state = new MasterToolState();
            }

            var catalog = new MasterTypeCatalog();
            var model = new MasterToolModel(state, catalog);
            var runner = new ProjectImportRunner(
                pipeline: new MasterImportPipeline(logger: new ConsoleMasterLogger()));
            var view = new MasterToolView(model, null, runner);
            rootVisualElement.Clear();
            rootVisualElement.Add(view.Build());
        }
    }
}
