using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase.Command;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Sicentury.Core.Collections;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Event
{
    public class EventViewModel : BaseModel
    {
        #region Variables

        /// <summary>
        /// 首次查询。需要计算总页码等参数。
        /// </summary>
        private const int QUERY_FIRST_TIME = -1;

        /// <summary>
        /// 一次性查询所有数据，EXCEL导出使用。
        /// </summary>
        private const int QUERY_FOR_EXCEL_EXPORT = 0;

        /// <summary>
        /// 指示函数<see cref="tbLoadPort1Selection"/>是否发生了重入。如果发生重入，说明“ALL”由
        /// 后台程序勾选，此时不要进行任何逻辑操作，仅设置”ALL“的选择状态。
        /// 重入的情况发生在：
        /// 1. 所有选项全部被选中
        /// 2. 此时将“ALL”以外的选项勾掉
        /// 3. “ALL”自动取消选择
        /// 上述2会触发3，引起函数重入。
        /// </summary>
        private bool _isEventSourceFilterSelectionReentered = false;

        private int _currentPage;
        private int _totalPage;
        private int _selectedPage;
        private int _selectedPaginationCapacity;
        private bool _isLoading;
        private bool _isExporting;
        private string _exportingMessage;

        /// <summary>
        /// 显示查询到的事件日志列表。
        /// </summary>
        private readonly IProgress<List<EventItem>> _progShowSearchingResult;

        /// <summary>
        /// 构造Event Source列表。
        /// </summary>
        private readonly IProgress<List<string>> _progConstructEventSourcesList;

        /// <summary>
        /// 更新分页信息。
        /// <para>Tuple.Item1: 当前页号</para>
        /// <para>Tuple.Item2: 总页号；设置为-1时表示忽略更新总页号。</para>
        /// </summary>
        private readonly IProgress<Tuple<int, int>> _progUpdatePaginationInfo;

        /// <summary>
        /// 更新查询状态。
        /// </summary>
        private readonly IProgress<bool> _progUpdateQueryStatus;

        /// <summary>
        /// 更新日志导出状态。
        /// <para>Tuple.Item1: 导出进度，0~100</para>
        /// <para>Tuple.Item2: 消息</para>
        /// </summary>
        private readonly IProgress<Tuple<int, string>> _progUpdateExportingState;

        #endregion

        #region Constructors

        public EventViewModel()
        {
            DisplayName = "Event";

            QueryEventList = () =>
            {
                var result = new List<string>();

                foreach (var eventName in Enum.GetNames(typeof(EventEnum)))
                    result.Add(eventName);

                return result;
            };

            SearchedResult = new ObservableRangeCollection<SystemLogItem>();
            FilterEventSources = new ObservableRangeCollection<string>();
            SelectedFilterEventSource = new ObservableRangeCollection<string>();
            PaginationSource = new ObservableRangeCollection<int>();

            SearchKeyWords = string.Empty;

            SearchAlarmEvent = true;
            SearchWarningEvent = true;
            SearchInfoEvent = true;
            SearchOpeLog = false;

            SearchPMA = false;
            SearchPMB = false;
            SearchPMC = false;
            SearchPMD = false;
            SearchTM = false;
            SearchLL = false;
            SearchSystem = false;
            
            tbLoadPort1SelectionChangedCommand = new BaseCommand<object>(tbLoadPort1Selection);
            

            // 分页支持的每页Log数量
            //! 最小每页条目数使用30，以保证正好显示一页而不出现纵向滚动条。
            PaginationCapacity = new List<int>
            {
                30,
                300,
                3000
            };

            _selectedPaginationCapacity = PaginationCapacity.Last();
            _totalPage = 1;
            _currentPage = 1;

            NavigateCommand = new BaseCommand<string>(Navigate, (o) => true);

            #region 显示查询到的EventLog列表

            _progShowSearchingResult = new Progress<List<EventItem>>((queryRet =>
            {
                if (SearchedResult == null)
                    SearchedResult = new ObservableRangeCollection<SystemLogItem>();
                else
                    SearchedResult.Clear();

                SearchedResult.AddRange(queryRet.Select(x =>
                    new SystemLogItem()
                    {
                        Time = (x.OccuringTime).ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        LogType = x.Level.ToString(),
                        Detail = x.Description,
                        TargetChamber = x.Source,
                        Initiator = "",
                        Icon = new BitmapImage(new Uri(
                            $"pack://application:,,,/MECF.Framework.UI.Core;component/Resources/SystemLog/{x.Level}.png",
                            UriKind.Absolute))
                    }
                ));
            }));

            #endregion

            #region 构造Event Sources列表

            _progConstructEventSourcesList = new Progress<List<string>>((list =>
            {
                SelectedFilterEventSource.Clear();
                FilterEventSources.Clear();

                SelectedFilterEventSource.Add("ALL");
                FilterEventSources.Add("ALL");

                SelectedFilterEventSource.AddRange(list);
                FilterEventSources.AddRange(list);

            }));

            #endregion

            #region 更新分页信息

            _progUpdatePaginationInfo = new Progress<Tuple<int, int>>(pageInfo =>
            {
                _currentPage = pageInfo.Item1;

                if (pageInfo.Item2 > 0)
                {
                    _totalPage = pageInfo.Item2;
                    PaginationSource.Clear();
                    PaginationSource.AddRange(Enumerable.Range(1, _totalPage));
                    NotifyOfPropertyChange(nameof(PaginationSource));
                }

                NotifyOfPropertyChange(nameof(PageInfo));

                _selectedPage = _currentPage;
                NotifyOfPropertyChange(nameof(SelectedPage));
            });

            #endregion

            #region 更新查询状态

            _progUpdateQueryStatus = new Progress<bool>(isBusy =>
            {
                IsLoading = isBusy;
            });

            #endregion

            #region 更新日志导出进度信息

            _progUpdateExportingState = new Progress<Tuple<int, string>>(state =>
            {
                var progress = state.Item1;
                var message = state.Item2;

                switch (progress)
                {
                    case 0:
                        IsExporting = true;
                        break;

                    case 100:
                        IsExporting = false;
                        break;
                }

                ExportingMessage = message;
            });

            #endregion
        }

        #endregion

        #region Properties

        public bool SearchAlarmEvent { get; set; }

        public bool SearchWarningEvent { get; set; }

        public bool SearchInfoEvent { get; set; }

        public bool SearchOpeLog { get; set; }

        public bool SearchPMA { get; set; }

        public bool SearchPMB { get; set; }

        public bool SearchPMC { get; set; }

        public bool SearchPMD { get; set; }

        //public bool SearchCoolDown { get; set; }
        public bool SearchTM { get; set; }

        public bool SearchLL { get; set; }

        //public bool SearchBuf1 { get; set; }
        public bool SearchSystem { get; set; }

        public string SearchKeyWords { get; set; }

        public string FilterKeyWords { get; set; }

        public DateTime SearchBeginTime
        {
            get => ((EventView)view).wfTimeFrom.Value;
            set
            {
                ((EventView)view).wfTimeFrom.Value = value;
                NotifyOfPropertyChange(nameof(SearchBeginTime));
            }
        }

        public DateTime SearchEndTime
        {
            get => ((EventView)view).wfTimeTo.Value;
            set
            {
                ((EventView)view).wfTimeTo.Value = value;
                NotifyOfPropertyChange(nameof(SearchEndTime));
            }
        }

        public List<string> EventList { get; set; }

        public string SelectedEvent { get; set; }

        public ObservableRangeCollection<SystemLogItem> SearchedResult { get; private set; }

        public Func<List<string>> QueryEventList { get; set; }

        public bool IsPermission => Permission == 3;

        private EventView view;

        public ObservableRangeCollection<string> FilterEventSources { get; set; }

        public ObservableRangeCollection<string> SelectedFilterEventSource { get; set; }

        public ICommand tbLoadPort1SelectionChangedCommand { get; set; }
        //public ICommand tbLoadPort2SelectionChangedCommand { get; set; }

        public ICommand NavigateCommand { get; set; }

        /// <summary>
        /// 返回受支持的分页每页容量。
        /// </summary>
        public List<int> PaginationCapacity { get; }

        /// <summary>
        /// 页码列表，用于快速跳转页面下拉框数据源。
        /// </summary>
        public ObservableRangeCollection<int> PaginationSource { get; }

        public int SelectedPage
        {
            get => _selectedPage;
            set
            {
                _selectedPage = value;
                Query(_selectedPage);
            }
        }

        /// <summary>
        /// 返回选择的分页每页容量。
        /// </summary>
        public int SelectedPaginationCapacity
        {
            get => _selectedPaginationCapacity;
            set
            {
                _selectedPaginationCapacity = value;
                Query();
            }
        }

        /// <summary>
        /// 页码信息。
        /// </summary>
        public string PageInfo => $"{_currentPage}/{_totalPage}";

        /// <summary>
        /// 返回是否正在加载数据。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(nameof(IsLoading));
            }
        }

        /// <summary>
        /// 是否正在导出数据。
        /// </summary>
        public bool IsExporting
        {
            get => _isExporting;
            private set
            {
                _isExporting = value;
                NotifyOfPropertyChange(nameof(IsExporting));
            }
        }

        /// <summary>
        /// 导出进度消息。
        /// </summary>
        public string ExportingMessage
        {
            get => _exportingMessage;
            private set
            {
                _exportingMessage = value;
                NotifyOfPropertyChange(nameof(ExportingMessage));
            }
        }

        #endregion

        #region Methods

        public void Navigate(string args)
        {
            switch (args)
            {
                case "first":
                    if (_currentPage == 1)
                        return;
                    _currentPage = 1;
                    break;

                case "previous":
                    if(_currentPage > 1)
                        _currentPage--;
                    break;

                case "next":
                    if(_currentPage < _totalPage)
                        _currentPage++;
                    break;

                case "last":
                    if (_currentPage == _totalPage)
                        return;
                    _currentPage = _totalPage;
                    break;

                default:
                    return;
            }

            Query(_currentPage);
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            // 默认的查询时间设置为当天
            this.view = (EventView)view;
            SearchBeginTime = DateTime.Today;
            SearchEndTime = DateTime.Today.AddDays(1).AddTicks(-1);
            Preload();
        }

        public void Preload()
        {
            EventList = new List<string> { "All" };

            if (QueryEventList != null)
            {
                var evList = QueryEventList();
                foreach (var ev in evList)
                    EventList.Add(ev);
            }

            SelectedEvent = "All";
            
            Query();
        }


        /// <summary>
        /// 打开YALV日志管理器。
        /// </summary>
        public void OpenDetailedLogViewer()
        {
            try
            {
                // 检查YALV是否已经打开
                var yalv = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.ToLower().Contains("yalv"));
                if (yalv != null)
                    throw new InvalidOperationException("日志管理器已经打开。");

                const string PATH_YALV = @"yalv\yalv.exe";
                var yalvPath = Path.Combine(Environment.CurrentDirectory, PATH_YALV);

                // check the existence of the YALV
                if (File.Exists(yalvPath) == false)
                {
                    throw new FileNotFoundException("日志管理器可执行文件不存在。");
                }

                var logFile = Path.Combine(Environment.CurrentDirectory, $"logs\\log{DateTime.Now:yyyyMMdd}.xlog");

                var proc = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = yalvPath,
                        Arguments = $"\"{logFile}\""
                    }
                };

                proc.Start();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                MessageBox.Show($"打开日志管理器失败，{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event Source中全选和全不选逻辑实现。
        /// <see cref="_isEventSourceFilterSelectionReentered"/>
        /// </summary>
        /// <param name="o"></param>
        public void tbLoadPort1Selection(object o)
        {
            if (_isEventSourceFilterSelectionReentered)
                return;

            _isEventSourceFilterSelectionReentered = true;

            if (o is ItemSelectionData item)
            {
                if (item.SelectItem == "ALL")
                {
                    //if (isAllSelectedInBackend)
                    //    return;

                    if (item.IsSelect)
                    {
                        SelectedFilterEventSource.Clear();
                        SelectedFilterEventSource.AddRange(FilterEventSources);
                        //FilterEventSources.ToList().ForEach(sp =>
                        //{
                        //    if (!SelectedFilterEventSource.Contains(sp)) 
                        //        SelectedFilterEventSource.Add(sp);
                        //});
                    }
                    else
                    {
                        SelectedFilterEventSource.Clear();
                    }
                }
                else
                {
                    // 判断是否需要取消或选中“ALL”
                    var list = 
                        FilterEventSources
                            .Except(new[] {"ALL"})
                            .Except(SelectedFilterEventSource.Except(new [] {"ALL"}))
                            .ToList();

                    //isAllSelectedInBackend = true;

                    if (list.Any())
                    {
                        if (SelectedFilterEventSource.Contains("ALL"))
                            SelectedFilterEventSource.Remove("ALL");

                    }
                    else
                    {
                        SelectedFilterEventSource.Clear();
                        SelectedFilterEventSource.AddRange(FilterEventSources);
                        //FilterEventSources.ToList().ForEach(sp =>
                        //{
                        //    if (!SelectedFilterEventSource.Contains(sp))
                        //        SelectedFilterEventSource.Add(sp);
                        //});
                    }

                    //isAllSelectedInBackend = false;
                }
            }

            _isEventSourceFilterSelectionReentered = false;
        }
        
        private string GetSourceWhere()
        {
            return "";
        }

        #region 数据库查询相关函数

        #region SQL 语句构造函数

        /// <summary>
        /// 获取EventLog查询语句中select部分表达式。
        /// </summary>
        private string GetEventLogQuerySql()
        {
            return
                "SELECT \"event_id\", \"event_enum\", \"type\", \"occur_time\", \"level\",\"source\" , \"description\" FROM \"event_data\"" +
                GetWhereTimeRangeExpression();
        }

        /// <summary>
        /// 获取EventLog行数查询语句中select部分表达式。
        /// </summary>
        private string GetEventLogCountSql()
        {
            return
                "SELECT COUNT(1) FROM \"event_data\"" + GetWhereTimeRangeExpression(); ;
        }

        /// <summary>
        /// 获取EventLog行数查询语句中select部分表达式。
        /// </summary>
        private string GetChamberListSql()
        {
            return
                "SELECT DISTINCT \"source\" FROM \"event_data\"" + GetWhereTimeRangeExpression();
        }

        /// <summary>
        /// 获取WHERE条件中的“时间范围”子句。
        /// <para>注意：所有Select语句构造时必须包含TimeRange条件。</para>
        /// </summary>
        /// <returns></returns>
        private string GetWhereTimeRangeExpression()
        {

            var sql = new StringBuilder(
                $" WHERE \"occur_time\" BETWEEN '{SearchBeginTime:yyyy/MM/dd HH:mm:ss}' AND '{SearchEndTime:yyyy/MM/dd HH:mm:ss}'");

            return sql.ToString();
        }

        /// <summary>
        /// 获取WHERE条件中的“Log Level”子句。
        /// </summary>
        /// <returns></returns>
        private string GetWhereLevelExpression()
        {
            var sqlLevelConditions = new List<string>();
            if (SearchInfoEvent)
                sqlLevelConditions.Add("'Information'");

            if (SearchWarningEvent)
                sqlLevelConditions.Add("'Warning'");

            if (SearchAlarmEvent)
                sqlLevelConditions.Add("'Alarm'");

            return $" AND (\"level\" in ({string.Join(",", sqlLevelConditions)}))";
        }

        /// <summary>
        /// 获取WHERE条件中的“关键字”子句。
        /// </summary>
        /// <returns></returns>
        private string GetWhereQueryKeyWordsExpression()
        {
            return string.IsNullOrEmpty(SearchKeyWords)
                ? ""
                : $" AND (lower(\"description\") like lower('%{SearchKeyWords}%'))";
        }

        /// <summary>
        /// 获取WHERE条件中的“事件类型过滤条件“子句。
        /// </summary>
        /// <returns></returns>
        private string GetWhereFilterEventSourceExpression()
        {
            if (SelectedFilterEventSource == null || SelectedFilterEventSource.Count <= 0)
                throw new ArgumentOutOfRangeException(nameof(SelectedFilterEventSource),
                    "no event source(s) in filter are selected.");

            if (SelectedFilterEventSource.Contains("ALL"))
                return "";

            return $" AND (\"source\" in ({string.Join(",", SelectedFilterEventSource.Select(x => $"'{x}'"))}))";
        }

        /// <summary>
        /// 获取WHERE条件中的“事件描述关键字过滤条件“子句。
        /// </summary>
        /// <returns></returns>
        private string GetWhereFilterEventKeywordExpression()
        {
            return string.IsNullOrEmpty(FilterKeyWords)
                ? ""
                : $" AND (lower(\"description\") like lower('%{FilterKeyWords}%'))";
        }

        /// <summary>
        /// 获取SQL查询语句中的分页表达式。
        /// </summary>
        /// <param name="countPerPage"></param>
        /// <param name="currentPage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private string GetPaginationExpression(int countPerPage, int currentPage)
        {
            if (countPerPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(countPerPage),
                    "the amount items per page can not be less than 1.");

            if (currentPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentPage),
                    "the current page required can not be less than 1.");



            return $" LIMIT {countPerPage} OFFSET {countPerPage * (currentPage - 1)}";
        }

        #endregion

        /// <summary>
        /// 查询当前条件下共有多少行数据。
        /// <param name="isIncludeFilter">是否包含过滤条件。</param>
        /// </summary>
        private int QueryRowAmount(bool isIncludeFilter)
        {
            var sql = GetEventLogCountSql() 
                      + GetWhereLevelExpression()
                      + GetWhereQueryKeyWordsExpression();

            if (isIncludeFilter)
                sql += GetWhereFilterEventSourceExpression()
                    + GetWhereFilterEventKeywordExpression();

            var dt = QueryDataClient.Instance.Service.QueryData(sql);

            // 未查询到任何数据。
            if (dt == null || dt.Rows.Count <= 0 || dt.Columns.Count <= 0)
                return 0;

            // 返回满足当前条件的行数。
            return int.Parse(dt.Rows[0][0].ToString());
        }

        /// <summary>
        /// 查询Event源列表。
        /// <para>注意：该查询仅在首次查询时使用；语句的条件仅只用时间范围和事件类型，不包含Filter选项。</para>
        /// <para>当套用Filter选项查询、或分页查询时，不再重构Event源列表。</para>
        /// </summary>
        /// <returns></returns>
        private List<string> QueryEventSources()
        {
            var chamberList = new List<string>();

            var sql = GetChamberListSql() + GetWhereLevelExpression();
            var dt = QueryDataClient.Instance.Service.QueryData(sql);

            // 未查询到任何数据。
            if (dt == null || dt.Rows.Count <= 0 || dt.Columns.Count <= 0)
                return chamberList;

            // 返回满足当前条件的行数。
            chamberList.AddRange(from DataRow row in dt.Rows select row[0].ToString());

            return chamberList;
        }

        /// <summary>
        /// 查询符合条件的事件日志。
        /// </summary>
        /// <param name="page">待查询页码。0:查询所有数据而不分页；其它：查询指定的页码。</param>
        /// <param name="isIncludeFilter">是否包含过滤条件。</param>
        /// <exception cref="ArgumentOutOfRangeException">页码错误，页码必须为大于等于0的整数。</exception>
        /// <returns></returns>
        private List<EventItem> QueryEventLogs(int page, bool isIncludeFilter)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), "the page number must be greater than 0.");

            var sql = GetEventLogQuerySql()
                      + GetWhereLevelExpression()
                      + GetWhereQueryKeyWordsExpression();

            if (isIncludeFilter)
                sql += GetWhereFilterEventSourceExpression()
                       + GetWhereFilterEventKeywordExpression();

            // page == 0 表示查询所有数据，仅导出Excel时使用。
            if(page > 0)
                sql += GetPaginationExpression(SelectedPaginationCapacity, page);

            return QueryDataClient.Instance.Service.QueryDBEvent(sql);
        }

        /// <summary>
        /// 查询数据。
        /// </summary>
        /// <param name="page">待查询的页号</param>
        /// <param name="isIncludeFilter">是否包含过滤条件。</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Query(int page = QUERY_FIRST_TIME, bool isIncludeFilter = false)
        {
            if (IsLoading)
                return;

            if (SearchBeginTime > SearchEndTime)
            {
                MessageBox.Show("Time range invalid, start time should be early than end time");
                return;
            }


            Task.Run(() =>
            {
                var searchingResult = new List<EventItem>();

                // 查询开始
                _progUpdateQueryStatus.Report(true);

                if (page <= QUERY_FIRST_TIME) // Query按钮按下，或首次加载界面
                {
                    // 统计总页数
                    var rowCount = QueryRowAmount(isIncludeFilter);

                    var totalPage = (int)Math.Ceiling(rowCount / (double)SelectedPaginationCapacity);

                    // 首次查询第一页
                    searchingResult = QueryEventLogs(1, isIncludeFilter);

                    if (isIncludeFilter == false)
                    {
                        // 构造Event Sources列表，仅在首次查询时构造
                        var eventSources = QueryEventSources();
                        _progConstructEventSourcesList.Report(eventSources);
                    }

                    // 更新页面信息
                    _progUpdatePaginationInfo.Report(new Tuple<int, int>(1, totalPage));


                }
                else if (page == QUERY_FOR_EXCEL_EXPORT)
                {

                }
                else if (page >= 1)
                {
                    // 翻页按钮按下
                    searchingResult = QueryEventLogs(page, isIncludeFilter);

                    // 更新页面信息
                    _progUpdatePaginationInfo.Report(new Tuple<int, int>(page, -1));

                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(page), "invalid page number.");
                }

                // 显示结果
                _progShowSearchingResult.Report(searchingResult);

                // 查询完毕
                _progUpdateQueryStatus.Report(false);

            });
        }

        /// <summary>
        /// 导出
        /// </summary>
        public async void Export()
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Operation Log_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                var result = dlg.ShowDialog(); // Show open file dialog box

                if (result != true) 
                    return;

                await Task.Run(() =>
                {

                    _progUpdateExportingState.Report(new Tuple<int, string>(0, "Querying data ..."));

                    // 查询数据
                    var queryRet = QueryEventLogs(0, false);
                    if (queryRet == null || queryRet.Count <= 0)
                        throw new Exception("no event logs are found, please change the conditions and retry.");


                    var ds = new DataSet();
                    ds.Tables.Add(new System.Data.DataTable("系统运行日志"));
                    ds.Tables[0].Columns.Add("Type");
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns.Add("System");
                    ds.Tables[0].Columns.Add("Content");

                    var currRow = 0;
                    var totalRow = queryRet.Count;
                    foreach (var item in queryRet)
                    {
                        currRow++;

                        _progUpdateExportingState.Report(new Tuple<int, string>(50,
                            $"Exporting item {currRow}/{totalRow} ..."));

                        var row = ds.Tables[0].NewRow();
                        row[0] = item.Level;
                        row[1] = item.OccuringTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
                        row[2] = item.Source;
                        row[3] = item.Description;
                        ds.Tables[0].Rows.Add(row);
                    }

                    _progUpdateExportingState.Report(new Tuple<int, string>(60, "Writing excel file ..."));
                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out var reason))
                        throw new InvalidOperationException($"Export failed, {reason}");

                    _progUpdateExportingState.Report(new Tuple<int, string>(100, "Done!"));
                });

                MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                MessageBox.Show("导出系统日志发生错误", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _progUpdateExportingState.Report(new Tuple<int, string>(100, "Done!"));
            }
        }

        #endregion

        #endregion
    }
}