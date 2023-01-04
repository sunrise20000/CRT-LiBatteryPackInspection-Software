/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\DataLogs\DataHistory\DataViewModel.cs
* @author Su Liang
* @Date 2022-08-01
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to optimize the performance.
*
* @details
*    1. 2-STAGE PIPELINE are introduced to separate the 'querying' and 'rendering' operations.
*    2. Querying day by day in separate thread.
* *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Core;
using MECF.Framework.UI.Client.CenterViews.Core.Charting;
#if !EXPORT_TO_CSV
using MECF.Framework.Common.Utilities;
#endif
using MECF.Framework.UI.Client.CenterViews.Operations.RealTime;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Extensions;
using SciChart.Core.Framework;
using SciChart.Data.Model;
using Sicentury.Core;
using Sicentury.Core.EventArgs;
using Sicentury.Core.Pipelines;
using Sicentury.Core.Tree;
using Cali = Caliburn.Micro;
using DateRange = SciChart.Data.Model.DateRange;
using MessageBox = System.Windows.MessageBox;
using TreeNode = Sicentury.Core.Tree.TreeNode;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.DataHistory
{

    public class DataViewModel : BusyIndicateableUiViewModelBase
    {
        #region Variables

        /// <summary>
        /// 一次最多查询的项目数。
        /// </summary>
        private const int MAX_ITEMS_PER_QUERY = 50;
        
        private IRange _timeRange;
        private IRange _visibleRangeValue;
        private AutoRange _autoRange;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 更新报告导出信息。
        /// </summary>
        private readonly IProgress<ProgressUpdatingEventArgs> _progCsvExport;

        /// <summary>
        /// 查询进度信息更新。
        /// </summary>
        private IProgress<ProgressUpdatingEventArgs> _progQueryUpdate;

        /// <summary>
        /// 挂起或恢复Chart更新
        /// </summary>
        private readonly IProgress<bool> _progChartSuspendUpdating;
        private IUpdateSuspender _suspender;

        /// <summary>
        /// 打开错误消息对话框。
        /// </summary>
        private readonly IProgress<string> _progShowErrorMessageBox;

        #endregion

        #region Constructors

        public DataViewModel()
        {
            DisplayName = "Data History";

            SelectedData = new ChartingLineSeriesCollection(DisplayName);

            var provider = new RealtimeProvider();
            ParameterNodes = new TreeNode(DisplayName)
            {
                MaxTerminalSelectionAllowed = MAX_ITEMS_PER_QUERY
            };
            ParameterNodes.ChildNodes.AddRange( provider.GetParameters());

            

            VisibleRangeTime = new DateRange(DateTime.Now.AddMinutes(-60), DateTime.Now.AddMinutes(60));
            VisibleRangeValue = new DoubleRange(0, 10);

            _progQueryUpdate = new Progress<ProgressUpdatingEventArgs>(e =>
            {
                if (e.CurrentProgress == e.TotalProgress)
                {
                    IsBusy = false;

                    if (_cancellationTokenSource?.Token.IsCancellationRequested == false)
                    {
                        foreach (var renderableSeries in SelectedData)
                        {
                            var series = renderableSeries as SicFastLineSeries;
                            var dataSeries = series?.GetDataSeries();

                            try
                            {
                                if (series != null && dataSeries != null)
                                {
                                    var node = series.BackendParameterNode;

                                    if (double.IsInfinity((double)dataSeries.YRange.Diff))
                                    {
                                        node.ClearStatistic();
                                    }
                                    else
                                    {
                                        var min = ((double)dataSeries.YMin);
                                        var max = ((double)dataSeries.YMax);
                                        var average =
                                            dataSeries.Metadata.Cast<ParameterNodePoint>().Average(x => x.Value);

                                        node.SetStatistic(min, max, average);
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                var err = $"It's failed to load data of {series?.DataName ?? "Unknown"}, {ex.Message}";
                                LOG.Error(err, ex);
                            }
                        }
                    }

                    ChartAutoRange = AutoRange.Never;
                    ((DataView)View).chart.ZoomExtents();
                   
                }

                BusyIndicatorContent = e.Message;
            });

            _progCsvExport = new Progress<ProgressUpdatingEventArgs>(e =>
            {
                BusyIndicatorContent = e.Message;
            });

            _progChartSuspendUpdating = new Progress<bool>(isSuspend =>
            {
                if (isSuspend)
                    _suspender = ((DataView)View).chart.SuspendSuspendUpdates();
                else
                {
                    try
                    {
                        if (_suspender?.IsSuspended == true)
                            _suspender?.SafeDispose();
                    }
                    catch (Exception)
                    {
                        // 查询过程最后会强制恢复挂起一次，可能引发异常，忽略此异常。
                    }

                }
            });

            _progShowErrorMessageBox = new Progress<string>((error =>
            {
                System.Windows.Forms.MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));
        }

        #endregion

        #region Property
        
        public  bool IsPermission => Permission == 3;

        public TreeNode ParameterNodes { get; }

        public ChartingLineSeriesCollection SelectedData { get; set; }

        public AutoRange ChartAutoRange
        {
            get => _autoRange;
            set
            {
                _autoRange = value;
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }

        public IRange VisibleRangeTime
        {
            get => _timeRange;
            set
            {
                _timeRange = value;
                NotifyOfPropertyChange(nameof(VisibleRangeTime));
            }
        }

        public IRange VisibleRangeValue
        {
            get => _visibleRangeValue;
            set
            {
                _visibleRangeValue = value;
                NotifyOfPropertyChange(nameof(VisibleRangeValue));
            }
        }

        public DateTime StartDateTime
        {
            get => ((DataView)View).wfTimeFrom.Value;
            set
            {
                ((DataView)View).wfTimeFrom.Value = value;
                NotifyOfPropertyChange(nameof(StartDateTime));
            }
        }

        public DateTime EndDateTime
        {
            get => ((DataView)View).wfTimeTo.Value;
            set
            {
                ((DataView)View).wfTimeTo.Value = value;
                NotifyOfPropertyChange(nameof(EndDateTime));
            }
        }

        #endregion

        #region Methods
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            StartDateTime = DateTime.Now.Date;
            EndDateTime = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// 查询数据。
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Query(bool isAppend = false)
        {
            if (StartDateTime > EndDateTime)
            {
                MessageBox.Show("time range invalid, start time should be early than end time.", "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // 哪些模组（TreeView的顶层Nodes）有选中项。
            var selectedModules = ParameterNodes.ChildNodes.Where(x => x.HasTerminalSelected).ToList();
            if (!selectedModules.Any())
            {
                MessageBox.Show($"No item(s) are selected to query.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            VisibleRangeTime = new DateRange(StartDateTime.AddHours(-1), EndDateTime.AddHours(1));
            ChartAutoRange = AutoRange.Always;

            BusyIndicatorContent = "Preparing list ...";
            IsBusy = true;

            _cancellationTokenSource = new CancellationTokenSource();
            
            #region 生成曲线列表

            var selectedTerminal =
                selectedModules.SelectMany(x => x.Flatten(true).Where(p => p.IsSelected == true)).ToList();
            List<IRenderableSeries> appendedSeries = null;

            if (isAppend)
                appendedSeries = SelectedData.Append(selectedTerminal);
            else
            {
                SelectedData.ReArrange(selectedTerminal);
                appendedSeries = SelectedData.ToList();
            }

            SelectedData.ResetColors();

            #endregion
            
            var dataSeriesList = appendedSeries.Select(x => x.DataSeries).ToList();

            Task.Run( async() =>
            {
                // 延时启动，等待UI准备好
                await Task.Delay(500);

                var pipeline = new TwoStagePipelineBasedTaskExecutor<DataSet, object>();

                pipeline.Stage2ActionStarted += (s, e) =>
                {
                    // 挂起Chart
                    _progChartSuspendUpdating.Report(true);
                };

                pipeline.Stage2ActionFinished += (s, e) =>
                {
                    // 挂起刷新
                    _progChartSuspendUpdating.Report(false);
                };

                pipeline.Stage1Finished += (s, e) =>
                {
                    // 查询完毕，Chart可能仍在渲染。
                    _progQueryUpdate?.Report(new ProgressUpdatingEventArgs(50, 100, "Still rendering chart ..."));
                };

                using (pipeline)
                {
                    var plTasks = pipeline.Start(null);

                    try
                    {
                        //! 按时间分段查询，解决查询速度慢导致卡后台业务的问题。
                        /*var ts = EndDateTime - StartDateTime;
                        if (ts.TotalDays <= 1)
                        {
                            Query(pipeline, selectedModules, dataSeriesList, StartDateTime, EndDateTime,
                                _cancellationTokenSource, _progQueryUpdate);

                            // 结束流水线
                            pipeline.AppendFunc1(null);
                            pipeline.AppendFunc2(null);
                        }
                        else
                        {*/
                            var daySlices =
                                DateRangeHelper.SplitInToHours(new DateRangeHelper(StartDateTime, EndDateTime), 12);
                           
                            
                            foreach (var range in daySlices)
                            {
                                Query(pipeline, selectedModules, dataSeriesList, range.Start, range.End,
                                    _cancellationTokenSource, _progQueryUpdate);

                                if (_cancellationTokenSource.Token.IsCancellationRequested)
                                    break;

                                await Task.Delay(1);
                            }

                            // 结束流水线
                            pipeline.AppendFunc1(null);
                            pipeline.AppendFunc2(null);

                        //}
                        
                        await Task.WhenAll(plTasks.ToArray());
                    }
                    catch (ThreadAbortException ex)
                    {
                        Thread.Sleep(500);
                        // 查询操作被取消。
                        Debug.WriteLine(ex);
                    }
                    catch (AggregateException ae)
                    {
                        var errs = new StringBuilder();
                        foreach (var ex in ae.Flatten().InnerExceptions)
                        {
                            LOG.Error(ex.Message, ex);
                            errs.AppendLine(ex.Message);
                        }

                        var errMsg = $"It's failed to query data, {errs}";
                        _progShowErrorMessageBox.Report(errMsg);
                        LOG.Error(errMsg, ae);
                    }
                    catch (Exception ex)
                    {
                        var errMsg = $"It's failed to query data, {ex.Message}";
                        _progShowErrorMessageBox.Report(errMsg);
                        LOG.Error(errMsg, ex);
                    }
                    finally
                    {
                        // 等待一下UI
                        Thread.Sleep(100);

                        _progQueryUpdate.Report(new ProgressUpdatingEventArgs(100, 100, ""));

                        // 强制恢复Chart刷新，避免异常情况导致的Chart挂起。
                        _progChartSuspendUpdating.Report(false);
                    }

                   
                }
            });
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        private static void Query(TwoStagePipelineBasedTaskExecutor<DataSet, object> pipeline, IEnumerable<TreeNode> selectedModules, List<IDataSeries> dataSeriesList, DateTime startTime, DateTime endTime,
            CancellationTokenSource cancellation, IProgress<ProgressUpdatingEventArgs> progressReporter = null)
        {
            pipeline.AppendFunc1(()=> 
                SearchDataBaseAsync(
                    selectedModules, 
                    new DateRangeHelper(startTime, endTime),
                    cancellation,
                    progressReporter));

            pipeline.AppendFunc2(ds =>
            {
                RenderChartAndTable(ds, dataSeriesList, cancellation, progressReporter);
                return null;
            });
        }

        /// <summary>
        /// 取消查询。
        /// </summary>
        public void CancelQuery()
        {
            Task.Run(() =>
            {
                if (_cancellationTokenSource?.Token.CanBeCanceled == true)
                {
                    _cancellationTokenSource.Cancel();
                }

                Thread.Sleep(100);
                _progQueryUpdate.Report(new ProgressUpdatingEventArgs(100, 100, ""));
                _progChartSuspendUpdating.Report(false);
            });
          
            if (View is DataView view)
                view.dataGrid.CancelOperation();
        }

        /// <summary>
        /// 检查指定的表名是否存在。
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private static bool CheckTableExists(string tableName)
        {
            var sql =
                $"SELECT EXISTS ( SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = '{tableName}' );";
            var table = QueryDataClient.Instance.Service.QueryData(sql);
            if (table == null)
                return false;

            if (table.Rows.Count <= 0)
                return false;

            var value = table.Rows[0][0].ToString();
            if (value.ToLower() == "true")
                return true;

            return false;
        }

        /// <summary>
        /// 根据DataLog界面左侧项目树中选择的项目查询数据
        /// </summary>
        /// <returns></returns>
        private static DataSet SearchDataBaseAsync(
            IEnumerable<TreeNode> modules, DateRangeHelper dateRange,
            CancellationTokenSource cancellation = null,
            IProgress<ProgressUpdatingEventArgs> progressReporter = null)
        {

            var ds = new DataSet();

            using (cancellation?.Token.Register(Thread.CurrentThread.Abort, true))
            {
                // 遍历模组
                foreach (var module in modules)
                {
                    // 如果当前根节点下没有被选中的终端节点，则忽略
                    if (module.ChildNodes.FirstOrDefault(x => (bool)x.HasTerminalSelected) == null)
                        continue;

                    string tblNameWithoutDay;
                    if (module.Name.StartsWith("IO"))
                    {
                        tblNameWithoutDay = $"{module.Name}";
                    }
                    else if (module.Name.StartsWith("PM"))
                    {
                        tblNameWithoutDay = $"{module.Name}";
                    }
                    else
                    {
                        tblNameWithoutDay = $"System";
                    }


                    if (module.Name.StartsWith("IO"))
                    {
                        foreach (var subIo in module.ChildNodes)
                        {
                            if(!subIo.HasTerminalSelected)
                                continue;

                            tblNameWithoutDay = subIo.FullName;

                            // 如果不是IO节点，则根节点名即为数据库名=
                            var dt = SearchSingleDbTable(tblNameWithoutDay, subIo, dateRange, cancellation, progressReporter);
                            if (dt != null)
                                ds.Tables.Add(dt);
                        }
                    }
                    else
                    {
                        if (module.ChildNodes.FirstOrDefault(x => (bool)x.HasTerminalSelected) == null)
                            continue;

                        var dt = SearchSingleDbTable(tblNameWithoutDay, module, dateRange, cancellation,
                            progressReporter);
                        if (dt != null)
                            ds.Tables.Add(dt);
                    }
                }
            }

            return ds;
        }

        /// <summary>
        /// IO节点在数据库中的表名比较特殊，需要特殊处理。
        /// </summary>
        /// <param name="module"></param>
        /// <param name="dateRange"></param>
        /// <param name="cancellation"></param>
        /// <param name="progressReporter"></param>
        /// <returns></returns>
        private static DataTable SearchSingleDbTable(string tableName, TreeNode module, DateRangeHelper dateRange,
            CancellationTokenSource cancellation = null,
            IProgress<ProgressUpdatingEventArgs> progressReporter = null)
        {
            var sql = new StringBuilder();
            //! 因为数据库中按天拆表，无法一次性查询数据，需使用UNION合并多表查询，因此此处按天拼接SQL表达式
            // 最终SQL表达式结构为：
            // (select xx from date1.xx) union (select xx from date2.xx) union (select xx from date3.xx)
            // where time between xxx and xxx
            // order by time asc
            var ts = dateRange.Diff;
            for (var day = 0; day <= ts.Days; day++)
            {
                var tblName = $"{dateRange.Start.AddDays(day):yyyyMMdd}.{tableName}";

                // 检查表名是否存在，否则SQL执行出错。
                if (CheckTableExists(tblName))
                {

                    sql.Append("select \"time\" AS InternalTimeStamp");
                    var selectedParams = module.Flatten(true)
                        .Where(x => x.IsSelected == true);

                    // 添加待查询的列
                    foreach (var item in selectedParams)
                    {
                        sql.Append("," + $"\"{item}\"");
                    }

                    sql.Append($" from \"{tblName}\" ");

                    if (day < ts.Days)
                        sql.Append(" UNION ");
                }
            }

            // 所有表名不可用，可能是日期范围错误
            if (sql.Length <= 0)
            {
                return null;
            }
            sql.Append(
            $" where \"time\" between {dateRange.Start.Ticks} and {dateRange.End.Ticks} order by InternalTimeStamp asc");
            progressReporter?.Report(new ProgressUpdatingEventArgs(20, 100,
                $"Querying {dateRange}..."));

            if (cancellation?.Token.IsCancellationRequested == true)
                return null;

            // 查询数据并将返回的结果存储在DataSet中
            var dataTable = QueryDataClient.Instance.Service.QueryData(sql.ToString());

            if (cancellation?.Token.IsCancellationRequested == true)
                return null;

            //! 返回的 DataTable 可能不存在，原因是上述代码自动生成的表明可能不存在。
            if (dataTable == null)
                return null;

            dataTable.TableName = module.FullName;
            return dataTable;
        }
        
        /// <summary>
        /// 渲染图表。
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dataSeriesList"></param>
        /// <param name="cancellation"></param>
        /// <param name="progressReporter"></param>
        /// <exception cref="Exception"></exception>
        private static void RenderChartAndTable(
            DataSet ds, List<IDataSeries> dataSeriesList,
            CancellationTokenSource cancellation = null, 
            IProgress<ProgressUpdatingEventArgs> progressReporter = null)
        {
            if (ds == null || ds.Tables.Count <= 0)
                return;

            // 一个Table一个模组
            foreach (var table in ds.Tables.Cast<DataTable>())
            {
                if (table.Rows.Count <= 0)
                    continue;

                // 一列对应模组中的某个数据记录点
                foreach (var col in table.Columns.Cast<DataColumn>())
                {
                    // 忽略时间列
                    if (col.Ordinal == 0)
                        continue;


                    var fullName = col.ColumnName;

                    if (!(dataSeriesList.FirstOrDefault(x => x.SeriesName == fullName) is SicFastLineDataSeries dataSeries))
                        continue;

                    var rows = table.Rows;
                    var dateList = new List<DateTime>();
                    var valueList = new List<double>();
                    var metaList = new List<ParameterNodePoint>();

                    for (var i = 0; i < rows.Count; i++)
                    {
                        var date = new DateTime(long.Parse(rows[i][0].ToString()));
                        var cellValue = rows[i][col];
                        var value = double.NaN;

                        if (cellValue is bool b)
                            value = b ? 1 : 0;
                        else if (double.TryParse(cellValue.ToString(), out var num))
                            value = num;
                        else
                            value = 0;

                        dateList.Add(date);
                        valueList.Add(value * dataSeries.Factor + dataSeries.Offset);
                        metaList.Add(new ParameterNodePoint(date, value));

                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;
                    }

                    if (cancellation?.Token.IsCancellationRequested == true)
                        return;

                    dataSeries.Append(dateList, valueList, metaList);

                }

                if (cancellation?.Token.IsCancellationRequested == true)
                    return;
                
                // 每一轮更新完毕后会恢复Chart刷新，
                // 数据量太小时会频繁挂起和恢复Chart，导致Chart不刷新，需要稍等一下UI。
                Thread.Sleep(50);
            }
        }

        public void Exporting(object sender, EventArgs e)
        {
            BusyIndicatorContent = "Exporting Start ...";
            IsBusy = true;
        }

        public void Exported(object sender, EventArgs e)
        {
            IsBusy = false;
        } 
        
        public void Deleted(object sender, EventArgs e)
        {
            ((DataView)View).tvParameterNodes.ClearPresetGroupSelectionOnly();
        }

        public void ProgressUpdating(object sender, ProgressUpdatingEventArgs e)
        {
            _progCsvExport.Report(e);
        }
        
        #endregion
    }
}
