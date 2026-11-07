namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// プロジェクトの現状を踏まえて、インポートパイプラインを 1 回実行します。
    /// </summary>
    internal interface IImportRunner
    {
        /// <summary>
        /// 依頼を実行します。
        /// </summary>
        /// <param name="request">依頼です。</param>
        /// <returns>実行結果です。</returns>
        PipelineResult Run(PipelineRequest request);
    }

    /// <summary>
    /// 実行のたびにプロジェクトのマスタアセットを集め直して、パイプラインを実行します。
    /// </summary>
    internal sealed class ProjectImportRunner : IImportRunner
    {
        readonly ProjectMasterTableCollector collector;
        readonly MasterImportPipeline pipeline;

        /// <summary>
        /// ランナーを初期化します。
        /// </summary>
        /// <param name="collector">テーブルの集め方です。null の場合は新しいインスタンスです。</param>
        /// <param name="pipeline">パイプラインです。null の場合は新しいインスタンスです。</param>
        public ProjectImportRunner(ProjectMasterTableCollector collector = null, MasterImportPipeline pipeline = null)
        {
            this.collector = collector ?? new ProjectMasterTableCollector();
            this.pipeline = pipeline ?? new MasterImportPipeline();
        }

        /// <inheritdoc />
        public PipelineResult Run(PipelineRequest request)
        {
            return pipeline.Execute(request, collector.Collect());
        }
    }
}
