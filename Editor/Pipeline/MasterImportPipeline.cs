using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.EditorTools.Logging;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using UnityEditor;

namespace MasterDataTool.EditorTools.Pipeline
{
    /// <summary>
    /// JSON のインポート、検証、必要なら保存を、1 回で実行します。
    /// </summary>
    internal sealed class MasterImportPipeline
    {
        readonly IOverwriteConfirmation confirmation;
        readonly Func<string, MasterDataBase> loadExistingAsset;
        readonly IMasterLogger logger;

        /// <summary>
        /// パイプラインを初期化します。
        /// </summary>
        /// <param name="confirmation">上書き確認です。null の場合はエディタのダイアログです。</param>
        /// <param name="loadExistingAsset">保存先の既存アセットを読み込みます。null の場合は AssetDatabase です。</param>
        /// <param name="logger">実行ログです。null の場合は何も出しません。</param>
        public MasterImportPipeline(
            IOverwriteConfirmation confirmation = null,
            Func<string, MasterDataBase> loadExistingAsset = null,
            IMasterLogger logger = null)
        {
            this.confirmation = confirmation ?? new EditorOverwriteConfirmation();
            this.loadExistingAsset = loadExistingAsset ?? DefaultLoadExistingAsset;
            this.logger = logger ?? NullMasterLogger.Instance;
        }

        /// <summary>
        /// 依頼を実行します。依頼が null の場合だけ例外を投げます。
        /// </summary>
        /// <param name="request">依頼です。</param>
        /// <param name="projectTables">参照先テーブルです。null は空として扱います。</param>
        /// <returns>実行結果です。</returns>
        public PipelineResult Execute(PipelineRequest request, ProjectTables projectTables)
        {
            var watch = Stopwatch.StartNew();
            if (request == null)
            {
                // ログ用の結果は呼び出し元へ返さない。例外の種類は変えない。
                WriteEnd(Invalid("依頼がありません。"), Elapsed(watch));
                throw new ArgumentNullException(nameof(request));
            }

            WriteStart(request);
            var result = ExecuteWithRequest(request, projectTables);
            WriteEnd(result, Elapsed(watch));
            return result;
        }

        PipelineResult ExecuteWithRequest(PipelineRequest request, ProjectTables projectTables)
        {
            if (projectTables == null)
            {
                projectTables = ProjectTables.Empty;
            }

            string reason;
            if (!TryValidateRequest(request, out reason))
            {
                return Invalid(reason);
            }

            MethodInfo closed;
            try
            {
                var open = typeof(MasterImportPipeline).GetMethod(
                    "ExecuteTyped",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (open == null)
                {
                    return Unexpected(new MissingMethodException(typeof(MasterImportPipeline).FullName, "ExecuteTyped"));
                }

                closed = open.MakeGenericMethod(request.MasterType, request.RecordType);
            }
            catch (ArgumentException ex)
            {
                return Invalid(ex.GetType().Name + ": " + ex.Message);
            }
            catch (Exception ex)
            {
                return Unexpected(ex);
            }

            try
            {
                return (PipelineResult)closed.Invoke(
                    null,
                    new object[] { request, projectTables, confirmation, loadExistingAsset });
            }
            catch (TargetInvocationException ex)
            {
                return Unexpected(ex.InnerException ?? ex);
            }
            catch (Exception ex)
            {
                return Unexpected(ex);
            }
        }

        static PipelineResult ExecuteTyped<TMaster, TRecord>(
            PipelineRequest request,
            ProjectTables projectTables,
            IOverwriteConfirmation confirmation,
            Func<string, MasterDataBase> loadExistingAsset)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            try
            {
                var import = MasterJsonImporter.ImportFromFile<TRecord>(request.JsonPath);
                var converted = ImportIssueConverter.ConvertAll(
                    import.Issues,
                    request.MasterType.Name,
                    request.AssetPath);
                var table = MasterTable.FromImport<TMaster, TRecord>(import, request.AssetPath);

                if (!import.Succeeded)
                {
                    var failed = Combine(converted, projectTables.Warnings, null, null);
                    return new PipelineResult(
                        PipelineStatus.ImportFailed,
                        failed,
                        table,
                        false,
                        null,
                        string.Empty);
                }

                var extraWarnings = new List<ValidationIssue>();
                var tables = BuildReferenceTables(projectTables.Tables, table, extraWarnings);
                var context = new ValidationContext(tables, new[] { table });
                var runnerResult = new ValidationRunner(DefaultValidators.Create()).Run(context);
                var validation = Combine(converted, projectTables.Warnings, extraWarnings, runnerResult);
                if (!validation.IsValid)
                {
                    return new PipelineResult(
                        PipelineStatus.ValidationFailed,
                        validation,
                        table,
                        false,
                        null,
                        string.Empty);
                }

                if (request.Mode == PipelineMode.Validate)
                {
                    return new PipelineResult(
                        PipelineStatus.Validated,
                        validation,
                        table,
                        true,
                        null,
                        string.Empty);
                }

                return Save<TMaster, TRecord>(
                    request,
                    import,
                    table,
                    validation,
                    confirmation,
                    loadExistingAsset);
            }
            catch (Exception ex)
            {
                return Unexpected(ex);
            }
        }

        static PipelineResult Save<TMaster, TRecord>(
            PipelineRequest request,
            ImportResult<TRecord> import,
            MasterTable table,
            ValidationResult validation,
            IOverwriteConfirmation confirmation,
            Func<string, MasterDataBase> loadExistingAsset)
            where TMaster : MasterDataBase<TRecord>
            where TRecord : MasterRecordBase
        {
            var options = new MasterAssetSaveOptions();
            var existing = loadExistingAsset(request.AssetPath);
            if (existing != null && request.MasterType.IsInstanceOfType(existing))
            {
                var approved = confirmation.Confirm(
                    request.AssetPath,
                    existing.Count,
                    table.Records.Count);
                if (!approved)
                {
                    return new PipelineResult(
                        PipelineStatus.SaveCancelled,
                        validation,
                        table,
                        true,
                        null,
                        string.Empty);
                }

                options.ExistingAsset = OverwritePolicy.Overwrite;
            }

            var saved = MasterAssetSaver.Save<TMaster, TRecord>(import, request.AssetPath, options);
            var info = new PipelineSaveInfo(
                saved.Outcome,
                saved.AssetPath,
                saved.PreviousRecordCount,
                saved.NewRecordCount,
                saved.Issues);
            var status = saved.Outcome == SaveOutcome.Failed
                ? PipelineStatus.SaveFailed
                : PipelineStatus.Saved;
            return new PipelineResult(status, validation, table, true, info, string.Empty);
        }

        static List<MasterTable> BuildReferenceTables(
            IReadOnlyList<MasterTable> projectTables,
            MasterTable current,
            List<ValidationIssue> extraWarnings)
        {
            var tables = new List<MasterTable>();
            var removed = new List<MasterTable>();
            if (projectTables != null)
            {
                for (var i = 0; i < projectTables.Count; i++)
                {
                    var table = projectTables[i];
                    if (table == null)
                    {
                        continue;
                    }

                    if (table.MasterType == current.MasterType)
                    {
                        continue;
                    }

                    if (string.Equals(table.Name, current.Name, StringComparison.Ordinal))
                    {
                        removed.Add(table);
                        continue;
                    }

                    tables.Add(table);
                }
            }

            if (removed.Count > 0)
            {
                extraWarnings.Add(CreateAmbiguousNameWarning(current, removed));
            }

            tables.Add(current);
            return tables;
        }

        static ValidationIssue CreateAmbiguousNameWarning(MasterTable current, List<MasterTable> removed)
        {
            var fullNames = new List<string>();
            if (current.MasterType != null)
            {
                fullNames.Add(current.MasterType.FullName);
            }

            for (var i = 0; i < removed.Count; i++)
            {
                var type = removed[i].MasterType;
                fullNames.Add(type == null ? removed[i].Name : type.FullName);
            }

            fullNames.Sort(StringComparer.Ordinal);
            var joined = string.Empty;
            for (var i = 0; i < fullNames.Count; i++)
            {
                if (i > 0)
                {
                    joined += ", ";
                }

                joined += fullNames[i];
            }

            var message = "同じ名前 '" + current.Name + "' のマスタ型が複数あるため(" + joined +
                          ")、参照先として使えません。";
            return new ValidationIssue(
                ValidationSeverity.Warning,
                ValidationIssueKind.AmbiguousMasterName,
                ProjectMasterTableCollector.ValidatorId,
                message,
                current.Name,
                FirstRemovedAssetPath(removed));
        }

        static string FirstRemovedAssetPath(List<MasterTable> removed)
        {
            MasterTable first = null;
            for (var i = 0; i < removed.Count; i++)
            {
                var table = removed[i];
                if (table == null || string.IsNullOrEmpty(table.AssetPath))
                {
                    continue;
                }

                if (first == null || CompareRemovedByTypeName(table, first) < 0)
                {
                    first = table;
                }
            }

            return first == null ? null : first.AssetPath;
        }

        static int CompareRemovedByTypeName(MasterTable left, MasterTable right)
        {
            if (left.MasterType == null && right.MasterType == null)
            {
                return 0;
            }

            if (left.MasterType == null)
            {
                return 1;
            }

            if (right.MasterType == null)
            {
                return -1;
            }

            return string.CompareOrdinal(left.MasterType.FullName, right.MasterType.FullName);
        }

        static ValidationResult Combine(
            IReadOnlyList<ValidationIssue> converted,
            IReadOnlyList<ValidationIssue> projectWarnings,
            IReadOnlyList<ValidationIssue> extraWarnings,
            ValidationResult runnerResult)
        {
            var results = new List<ValidationResult>();
            results.Add(new ValidationResult(converted ?? new ValidationIssue[0]));
            results.Add(new ValidationResult(projectWarnings ?? new ValidationIssue[0]));
            if (extraWarnings != null)
            {
                results.Add(new ValidationResult(extraWarnings));
            }

            if (runnerResult != null)
            {
                results.Add(runnerResult);
            }

            return ValidationResult.Combine(results);
        }

        static bool TryValidateRequest(PipelineRequest request, out string reason)
        {
            if (request.MasterType == null)
            {
                reason = "マスタ型が指定されていません。";
                return false;
            }

            if (request.RecordType == null)
            {
                reason = "レコード型が指定されていません。";
                return false;
            }

            if (request.MasterType.IsAbstract)
            {
                reason = "マスタ型 '" + request.MasterType.Name + "' は abstract のため、インポートできません。";
                return false;
            }

            if (!typeof(MasterRecordBase).IsAssignableFrom(request.RecordType))
            {
                reason = "レコード型 '" + request.RecordType.Name + "' は、MasterRecordBase を継承していません。";
                return false;
            }

            if (!IsMatchingMaster(request.MasterType, request.RecordType))
            {
                reason = "マスタ型 '" + request.MasterType.Name + "' は、MasterDataBase<" +
                         request.RecordType.Name + "> を継承していません。";
                return false;
            }

            if (string.IsNullOrEmpty(request.JsonPath))
            {
                reason = "JSON ファイルのパスが空です。";
                return false;
            }

            if (string.IsNullOrEmpty(request.AssetPath))
            {
                reason = "保存先のアセットパスが空です。";
                return false;
            }

            reason = null;
            return true;
        }

        static bool IsMatchingMaster(Type masterType, Type recordType)
        {
            for (var current = masterType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                {
                    continue;
                }

                if (current.GetGenericTypeDefinition() != typeof(MasterDataBase<>))
                {
                    continue;
                }

                return current.GetGenericArguments()[0] == recordType;
            }

            return false;
        }

        static MasterDataBase DefaultLoadExistingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<MasterDataBase>(assetPath);
        }

        void WriteStart(PipelineRequest request)
        {
            try
            {
                logger.Log(MasterLogLevel.Info, MasterRunLogFormatter.FormatStart(request));
            }
            catch (Exception)
            {
                // ログの失敗で実行結果を変えない。
            }
        }

        void WriteEnd(PipelineResult result, long elapsedMilliseconds)
        {
            try
            {
                logger.Log(
                    MasterRunLogFormatter.GetEndLevel(result),
                    MasterRunLogFormatter.FormatEnd(result, elapsedMilliseconds));
            }
            catch (Exception)
            {
                // ログの失敗で実行結果を変えない。
            }
        }

        static long Elapsed(Stopwatch watch)
        {
            var elapsed = watch.ElapsedMilliseconds;
            return elapsed < 0 ? 0 : elapsed;
        }

        static PipelineResult Invalid(string message)
        {
            return new PipelineResult(
                PipelineStatus.InvalidRequest,
                ValidationResult.Empty,
                null,
                false,
                null,
                message);
        }

        static PipelineResult Unexpected(Exception exception)
        {
            var message = exception.GetType().Name + ": " + exception.Message;
            return new PipelineResult(
                PipelineStatus.UnexpectedError,
                ValidationResult.Empty,
                null,
                false,
                null,
                message);
        }
    }
}
