using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Caliburn.Micro.Core;
using MECF.Framework.Common.ControlDataContext;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Common.Helpers;
using SciChart.Charting.ViewportManagers;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Cali = Caliburn.Micro;

namespace SicUI.Models.PMs.Charting
{
    public class RecipeItemSic : RecipeItem
    {
        public string SyncStep { get; set; }
        public string ProcessGuid { get; set; }
    }

    [Serializable]
    public class PMNodeInfo : PropertyChangedBase
    {
        private bool _selected = false;
        public bool Selected
        {
            get { return _selected; }
            set 
            {
                _selected = value;
                NotifyOfPropertyChange("Selected");
            }
        }

        public string NodeStr { get; set; }


        private bool _isMatch = true;
        public bool IsMatch
        {
            get { return _isMatch; }
            set { _isMatch = value; NotifyOfPropertyChange("IsMatch"); }
        }

        public void ApplyCriteria(string criteria)
        {
            if (IsCriteriaMatched(criteria))
            {
                IsMatch = true;
            }
            else
            {
                IsMatch = false;
            }
        }

        private bool IsCriteriaMatched(string criteria)
        {
            return  !string.IsNullOrEmpty(criteria) && NodeStr.ToLower().Contains(criteria.ToLower()) || string.IsNullOrEmpty(criteria);             
        }
    }

    public class DataDetail
    {
        public string ProcessGuid { get; set; }
        public double xValue { get; set; }
        public double yValue { get; set; }
    }

    public class PMChartingViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        private class QueryIndexer
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string Module { get; set; }
            public string ColumneName { get; set; }
            public string ProcessGuid { get; set; }
            public List<string> DateList { get; set; }
            public long currentTimeTicks { get; set; }
        }


        public PMChartingViewModel()
        {
            DisplayName = "Process History";

            DataDetailVisbleCommand = new ActionCommand(SetDataDetailVisble);
            DefaultZoomCommand = new ActionCommand(Zoom);
            ShowLegendCommand = new ActionCommand(SetLegendAvalible);
            ShowAlarmCommand = new ActionCommand(AddAlarmInfo);

            GetPMNode();

            SelectedData = new ObservableCollection<IRenderableSeries>();
            Recipes = new ObservableCollection<RecipeItemSic>();

            AutoRangeX = AutoRange.Always;
            AutoRangeY = AutoRange.Always;

            VisibleRangeX = new DoubleRange(0, 10000);
            VisibleRangeY = new DoubleRange(-10, 500);
            VisibleRangeXLimit = new DoubleRange(0, 10000);
            VisibleRangeYLimit = new DoubleRange(-10, 500);

            AddRangeAnnotation();

            _thread = new PeriodicJob(200, MonitorHistoryData, "History", true);
            _threadReal = new PeriodicJob(TrendInterval, MonitorRealTimeData, "RealTime", true);
        }

        private PeriodicJob _thread;
        private PeriodicJob _threadReal;
        //DeviceTimer dt = new DeviceTimer();
        private const int MAX_PARAMETERS = 20;
        public DateTime _queryDateTimeToken;

        private readonly IViewportManager _viewportManager = new DefaultViewportManager();
        public IViewportManager ViewportManager
        {
            get { return _viewportManager; }
        }

        #region Properties 
        [IgnorePropertyChange]
        public int TrendInterval { get; set; } = 5000;
        public bool IntervalSaved { get; set; }
        private DateTime _realTimeStart = DateTime.MaxValue;
        private string _realTimeGuid = "";
        private long _realTimeTick = 0;
        private List<string> _seletedItemName = new List<string>();

        ConcurrentBag<QueryIndexer> _lstTokenTimeData = new ConcurrentBag<QueryIndexer>();
        //ConcurrentBag<QueryIndexer> _lstRealTimeData = new ConcurrentBag<QueryIndexer>();
        //List<string> _lstRemoveRecipe = new List<string>();


        public ObservableCollection<DataDetail> QueryDataDetail { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData { get; set; }
        public ObservableCollection<RecipeItemSic> Recipes { get; set; }

        private AnnotationCollection _alarmAnnotation;
        public AnnotationCollection AlarmAnnotation
        {
            get { return _alarmAnnotation; }
            set
            {
                _alarmAnnotation = value;
                NotifyOfPropertyChange(nameof(AlarmAnnotation));
            }
        }

        private RecipeItemSic _selectedProcessData;
        public RecipeItemSic SelectedProcessData
        {
            get { return _selectedProcessData; }
            set
            {
                _selectedProcessData = value;
                NotifyOfPropertyChange(nameof(SelectedProcessData));
            }
        }

        private List<PMNodeInfo> _ConfigNodes = new List<PMNodeInfo>();
        public List<PMNodeInfo> ConfigNodes
        {
            get { return _ConfigNodes; }
            set { _ConfigNodes = value; NotifyOfPropertyChange("ConfigNodes"); }
        }

        private IRange _visibleRangeXLimit;
        public IRange VisibleRangeXLimit
        {
            get { return _visibleRangeXLimit; }
            set { _visibleRangeXLimit = value; NotifyOfPropertyChange(nameof(VisibleRangeXLimit)); }
        }

        private IRange _visibleRangeYLimit;
        public IRange VisibleRangeYLimit
        {
            get { return _visibleRangeYLimit; }
            set { _visibleRangeYLimit = value; NotifyOfPropertyChange(nameof(VisibleRangeYLimit)); }
        }

        private IRange _visibleRangeX;
        public IRange VisibleRangeX
        {
            get { return _visibleRangeX; }
            set { _visibleRangeX = value; NotifyOfPropertyChange(nameof(VisibleRangeX)); }
        }
        private IRange _visibleRangeY;
        public IRange VisibleRangeY
        {
            get { return _visibleRangeY; }
            set { _visibleRangeY = value; NotifyOfPropertyChange(nameof(VisibleRangeY)); }
        }

        public string UseSetX1
        {
            get;set;
        }
        public string UseSetX2
        {
            get; set;
        }
        public string UseSetY1
        {
            get; set;
        }
        public string UseSetY2
        {
            get; set;
        }
       
        private AutoRange _autoRangeX;
        public AutoRange AutoRangeX
        {
            get => _autoRangeX;
            set { _autoRangeX = value; NotifyOfPropertyChange(nameof(AutoRangeX)); }
        }

        private AutoRange _autoRangeY;
        public AutoRange AutoRangeY
        {
            get => _autoRangeY;
            set { _autoRangeY = value; NotifyOfPropertyChange(nameof(AutoRangeY)); }
        }

        private bool _enableAutoZoom = true;
        public bool EnableAutoZoom
        {
            get { return _enableAutoZoom; }

            set
            {
                _enableAutoZoom = value;
                NotifyOfPropertyChange(nameof(EnableAutoZoom));
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }


        private AutoRange _chartAutoRange = AutoRange.Never;
        public AutoRange ChartAutoRange
        {
            get { return _chartAutoRange; }
            set
            {
                _chartAutoRange = value;
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }

        private bool _realTimeMode;
        public bool RealTimeMode
        {
            get { return _realTimeMode; }
            set
            {
                _realTimeMode = value;
                NotifyOfPropertyChange(nameof(RealTimeMode));
                if (_realTimeMode)
                {
                    AddRealTimeInfo();
                }
                else
                {
                    ClearRealTimeInfo();
                }
            }
        }

        private string _currentCriteria = String.Empty;
        public string CurrentCriteria
        {
            get { return _currentCriteria; }
            set
            {
                if (value == _currentCriteria)
                    return;

                _currentCriteria = value;
                NotifyOfPropertyChange("CurrentCriteria");
                ApplyFilter();
            }
        }

        private double _realTimeX;
        public double RealTimeX
        {
            get { return _realTimeX; }
            set
            {
                _realTimeX = value;
                NotifyOfPropertyChange(nameof(RealTimeX));
            }
        }

        private double _thresholdX1;
        public double ThresholdX1
        {
            get { return _thresholdX1; }
            set
            {
                _thresholdX1 = value;
                NotifyOfPropertyChange(nameof(ThresholdX1));
            }
        }

        private double _thresholdX2;
        public double ThresholdX2
        {
            get { return _thresholdX2; }
            set
            {
                _thresholdX2 = value;
                NotifyOfPropertyChange(nameof(ThresholdX2));
            }
        }



        private bool _showLegendInfo;
        public bool ShowLegendInfo
        {
            get { return _showLegendInfo; }
            set
            {
                _showLegendInfo = value;
                NotifyOfPropertyChange(nameof(ShowLegendInfo));
            }
        }

        private Visibility _dataDetailVisbility = Visibility.Collapsed;
        public Visibility DataDetailVisbility
        {
            get { return _dataDetailVisbility; }
            set
            {
                _dataDetailVisbility = value;
                NotifyOfPropertyChange(nameof(DataDetailVisbility));
            }
        }

        private Dictionary<string, string> keyValuesAlarm = new Dictionary<string, string>();

        private Queue<Color> colorQueue = new Queue<Color>(new Color[]{Color.Red,Color.Orange,Color.Green,Color.Blue,Color.Purple,Color.Aqua,Color.Bisque,Color.Brown,Color.BurlyWood,Color.CadetBlue,
            Color.CornflowerBlue,Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen, Color.DarkOrange,
            Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray, Color.DodgerBlue,Color.ForestGreen, Color.Gold,
            Color.Gray,Color.GreenYellow,Color.HotPink,Color.Indigo,Color.Khaki,
            Color.LimeGreen,Color.MediumOrchid,Color.MediumPurple,Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,
            Color.MediumTurquoise,Color.Moccasin,Color.NavajoWhite,Color.Olive,Color.OliveDrab,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,
            Color.PeachPuff,Color.Peru,Color.Plum,Color.PowderBlue,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SeaGreen, Color.Sienna,
            Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.SpringGreen,Color.Teal,Color.Aquamarine,Color.Tomato,Color.Turquoise,Color.Violet,Color.Wheat, Color.YellowGreen});
        #endregion Properties


        Cali.WindowManager wm = new Cali.WindowManager();
        ChartingSelectDataViewModel selectDataDlg = new ChartingSelectDataViewModel();

        #region 界面按钮
        public void Add()
        {
            selectDataDlg.CheckAllRecipeFlag = false;
            selectDataDlg.SelectedRecipes.Clear();
            selectDataDlg.SelectedParameters.Clear();

            var settings = new Dictionary<string, object> { { "Title", "Select Recipe Data" } };
            bool? ret = wm.ShowDialog(selectDataDlg, null, settings);
            if (ret == null || !ret.Value)
                return;


            for (int j = SelectedData.Count - 1; j >= 0; j--)
            {
                if (!(SelectedData[j] as ChartDataLineX).RecName.Contains("RealTime"))
                {
                    SelectedData.RemoveAt(j);
                }
            }

            GetXRange();


            foreach (PMNodeInfo node in ConfigNodes)
            {
                if (node.Selected)
                {
                    ParameterCheck(node);
                }
            }
        }

        /// <summary>
        /// 删除所有的ProcessData信息
        /// </summary>
        public void RemoveAllProcessData()
        {
            Recipes.Clear();
            this.SelectedData.Clear();
            foreach (PMNodeInfo node in ConfigNodes)
            {
                node.Selected = false;
            }
        }

        /// <summary>
        /// 清除所有选择的节点
        /// </summary>
        public void RemoveAllLine()
        {
            this.SelectedData.Clear();
            foreach (PMNodeInfo node in ConfigNodes)
            {
                node.Selected = false;
            }
        }

        /// <summary>
        /// 删除单个ProcessData
        /// </summary>
        public void DeleteProcessData(RecipeItemSic item)
        {
            if (Recipes.Contains(item))
            {
                lock (_lockSelection)
                {
                    for (int j = SelectedData.Count - 1; j >= 0; j--)
                    {
                        if ((SelectedData[j] as ChartDataLineX).ProcessGuid.Contains(item.ProcessGuid))
                        {
                            SelectedData.RemoveAt(j);
                        }
                    }
                    
                }
                this.Recipes.Remove(item);
            }            
        }

        public void Template()
        {
            ChartingTemplateViewModel templateDataDlg = new ChartingTemplateViewModel();
            templateDataDlg.CurrentDataInfo = ConfigNodes;

            Cali.WindowManager wm = new Cali.WindowManager();
            bool? bret = wm.ShowDialog(templateDataDlg);
            if (!bret.HasValue || !bret.Value)
            {
                return;
            }
            else
            {
                ConfigNodes = templateDataDlg.CurrentDataInfo;

                if (SelectedData.Count > 0)
                {
                    lock (_lockSelection)
                    {
                        foreach (var node in ConfigNodes)
                        {
                            if (!node.Selected)
                            {
                                for (int j = SelectedData.Count - 1; j >= 0; j--)
                                {
                                    if (!(SelectedData[j] as ChartDataLineX).ProcessGuid.Contains(node.NodeStr))
                                    {
                                        SelectedData.RemoveAt(j);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var node in ConfigNodes)
                {
                    ParameterCheck(node);
                }
            }
        }

        private void ClearRealTimeInfo()
        {
            _realTimeStart = DateTime.MaxValue;
            lock (_lockSelection)
            {
                for (int i = SelectedData.Count - 1; i >= 0; i--)
                {
                    if ((SelectedData[i] as ChartDataLineX).RecName == "RealTime")
                    {
                        SelectedData.RemoveAt(i);
                    }
                }
            }
        }


        private void GetXRange()
        {
            double maxXValue = 0;
            foreach (var recipe in selectDataDlg.SelectedRecipes)
            {
                if (Recipes.FirstOrDefault(x => x.ProcessGuid == recipe.ProcessGuid) != null)
                {
                    continue;
                }
                this.Recipes.Add(recipe);

                long curTickCount = (DateTime.Parse(recipe.EndTime).Ticks - DateTime.Parse(recipe.StartTime).Ticks) / 10000000;
                if (maxXValue < curTickCount)
                {
                    maxXValue = curTickCount;
                }
            }

            VisibleRangeX = new DoubleRange(0, maxXValue);
            VisibleRangeXLimit = new DoubleRange(0, maxXValue);
        }

        private void AddRealTimeInfo()
        {
            _realTimeStart = DateTime.MaxValue;
            string sqlStr= String.Format("SELECT \"process_data\".\"process_begin_time\",\"process_data\".\"guid\" ,\"process_data\".\"process_in\" " +
                "FROM \"process_data\",\"cj_data\",\"pj_data\",\"wafer_data\" where  \"process_data\".\"wafer_data_guid\" = \"wafer_data\".\"guid\" " +
                "and \"wafer_data\".\"pj_data_guid\" = \"pj_data\".\"guid\" and \"cj_data\".\"guid\" = \"pj_data\".\"cj_data_guid\" " +
                "and \"process_data\".\"process_begin_time\" >= '{0}' and \"process_data\".\"process_end_time\" is null " +
                "order by \"process_data\".\"process_begin_time\" DESC", DateTime.Now.AddHours(-3).ToString("yyyy-MM-dd HH:mm:ss"));
            DataTable dataTable = QueryDataClient.Instance.Service.QueryData(sqlStr);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                _realTimeStart = (DateTime)dataTable.Rows[0]["process_begin_time"];
                _realTimeGuid = dataTable.Rows[0]["guid"].ToString();
                string _realTimeChamber = dataTable.Rows[0]["process_in"].ToString();

                DateTime _realHisEndTime = DateTime.Now;
                _realTimeTick = _realHisEndTime.Ticks+1;

                lock (_lockSelection)
                {
                    foreach (PMNodeInfo node in ConfigNodes)
                    {
                        if (node.Selected)
                        {
                            string nodeStr = _realTimeChamber + "." + node.NodeStr;                          //字段名称
                            string nodeWithGuid = node.NodeStr + "_" + _realTimeGuid;

                            bool isExist = SelectedData.FirstOrDefault(x => (x as ChartDataLineX).ProcessGuid == nodeWithGuid) != null;
                            if (!isExist)
                            {
                                var line = new ChartDataLineX(nodeStr);
                                line.ProcessGuid = nodeWithGuid;
                                line.RecName = "RealTime";
                                line.Tag = node;
                                SelectedData.Add(line);
                                SelectedDataChanged();
                                QueryIndexer indexer = _lstTokenTimeData.FirstOrDefault(x => x.ProcessGuid == line.ProcessGuid);

                                if (indexer == null)
                                {
                                    indexer = new QueryIndexer()
                                    {
                                        Module = line.Module,
                                        StartTime = _realTimeStart,
                                        EndTime = _realHisEndTime,
                                        ProcessGuid = line.ProcessGuid,
                                        ColumneName = nodeStr,
                                        DateList = GetDateList(_realTimeStart, _realHisEndTime)
                                    };

                                    _lstTokenTimeData.Add(indexer);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _realTimeStart = DateTime.MaxValue;
            }
        }
        #endregion 界面按钮




        private void SelectedDataChanged()
        {
            foreach (var item in SelectedData)
            {
                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                {
                    Color drawingColor = colorQueue.Peek();
                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                    colorQueue.Enqueue(colorQueue.Dequeue());
                }
            }
        }

        /// <summary>
        /// 加载PM的Device名称
        /// </summary>
        private void GetPMNode()
        {
            List<string> dataList = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");
            dataList.Sort();
            List<string> lstNode = new List<string>();
            foreach (string dataName in dataList)
            {
                string[] nodeName = dataName.Split('.');
                if (nodeName.Length > 1 && nodeName[0].IndexOf("PM") == 0)
                {
                    string nodeStr = dataName.Substring(dataName.IndexOf('.') + 1);
                    if (!lstNode.Contains(nodeStr))
                    {
                        ConfigNodes.Add(new PMNodeInfo() { NodeStr = nodeStr });
                    }
                }
            }
        }



        private object _lockSelection = new object();
        //private object _lockRealtime = new object();


        protected bool MonitorHistoryData()
        {
            try
            {
                if (_lstTokenTimeData.Count > 0)
                {
                    lock (_lockSelection)
                    {
                        foreach (var item in _lstTokenTimeData)
                        {
                            DateTime timeFrom = item.StartTime;
                            DateTime timeTo = item.EndTime;

                            GetHistoryData(item.DateList, timeFrom, timeTo, item.Module, item.ColumneName, item.ProcessGuid);
                            GetHistoryAlarmData(item.ProcessGuid, timeFrom, timeTo, item.Module);
                            _lstTokenTimeData.TryTake(out _);
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
            return true;
        }

        protected bool MonitorRealTimeData()
        {
            try
            {
                if (RealTimeMode && _realTimeStart < DateTime.Now && _lstTokenTimeData.Count == 0)
                {
                    lock (_lockSelection)
                    {
                        GetRealTimeData();
                    }
                }                
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
            return true;
        }

        private void GetRealTimeData()
        {
            long nowTickCount = DateTime.Now.Ticks;
            string colName = "";
            string module = "PM1";

            List<string> lstColumn = GetColumnList(module);
            if (lstColumn.Count <= 0)
            {
                return;
            }
            colName = String.Join(",", lstColumn);

            string sql = String.Format("select time AS InternalTimeStamp,{0}", colName);
            sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc",
                DateTime.Now.ToString("yyyyMMdd") + "." + module, _realTimeTick, nowTickCount);

            _realTimeTick = nowTickCount;

            Dictionary<string, List<DataDetail>> historyData = new Dictionary<string, List<DataDetail>>();
            
            DataTable dataTable = QueryDataClient.Instance.Service.QueryData(sql);
            if (dataTable == null || dataTable.Rows.Count == 0 || dataTable.Columns.Count < 2)
            {
                return;
            }

            RealTimeX = ((long)dataTable.Rows[dataTable.Rows.Count - 1][0] - _realTimeStart.Ticks) / 10000000;

            for (int j = 1; j < dataTable.Columns.Count; j++)
            {
                List<DataDetail> lstDataDetail = new List<DataDetail>();
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    DataDetail data = new DataDetail();
                    long ticks = (long)dataTable.Rows[i][0] - _realTimeStart.Ticks;
                    data.xValue = ticks / 10000000;

                    if (dataTable.Rows[i][j] is DBNull || dataTable.Rows[i][j] is null)
                    {
                        data.yValue = 0;
                    }
                    else if (dataTable.Rows[i][j] is bool)
                    {
                        data.yValue = (bool)dataTable.Rows[i][j] ? 1 : 0;
                    }
                    else
                    {
                        data.yValue = float.Parse(dataTable.Rows[i][j].ToString());
                    }
                    lstDataDetail.Add(data);
                }
                historyData.Add(dataTable.Columns[j].ColumnName.Replace("PM1.","")+"_"+ _realTimeGuid, lstDataDetail);
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    foreach (var item in SelectedData)
                    {
                        var seriesItem = item as ChartDataLineX;

                        if (seriesItem == null)
                            continue;
                        foreach (var data in historyData)
                        {
                            if (data.Key != seriesItem.ProcessGuid)
                                continue;

                            seriesItem.Capacity += data.Value.Count;

                            foreach (var detailDataItem in data.Value)
                            {
                                seriesItem.Append(detailDataItem.xValue, detailDataItem.yValue);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }));
        }

        private void GetHistoryData(List<string> lstDayStr, DateTime from, DateTime to, string module, string colName, string processGuid)
        {
            Dictionary<string, List<DataDetail>> historyData = new Dictionary<string, List<DataDetail>>();
            List<DataDetail> lstDataDetail = new List<DataDetail>();

            foreach (string cDate in lstDayStr)
            {
                string sql = String.Format("select time AS InternalTimeStamp,\"{0}\"", colName);
                sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc",
                    cDate + "." + module, from.Ticks, to.Ticks);

                DataTable dataTable = QueryDataClient.Instance.Service.QueryData(sql);
                if (dataTable == null || dataTable.Rows.Count == 0 || dataTable.Columns.Count < 2)
                {
                    return;
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    DataDetail data = new DataDetail();
                    long ticks = (long)dataTable.Rows[i][0] - from.Ticks;
                    data.xValue = ticks / 10000000;
                    if (dataTable.Rows[i][1] is DBNull || dataTable.Rows[i][1] is null)
                    {
                        data.yValue = 0;
                    }
                    else if (dataTable.Rows[i][1] is bool)
                    {
                        data.yValue = (bool)dataTable.Rows[i][1] ? 1 : 0;
                    }
                    else
                    {
                        data.yValue = float.Parse(dataTable.Rows[i][1].ToString());
                    }
                    lstDataDetail.Add(data);
                }
            }

            historyData.Add(processGuid, lstDataDetail);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    foreach (var item in SelectedData)
                    {
                        var seriesItem = item as ChartDataLineX;

                        if (seriesItem == null)
                            continue;
                        foreach (var data in historyData)
                        {
                            if (data.Key != seriesItem.ProcessGuid)
                                continue;

                            seriesItem.Capacity += data.Value.Count;

                            foreach (var detailDataItem in data.Value)
                            {
                                seriesItem.Append(detailDataItem.xValue, detailDataItem.yValue);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }));
        }

        private void GetHistoryAlarmData(string processguid,DateTime from, DateTime to, string module)
        {
            string sql = String.Format("select occur_time,description from public.event_data where source='{0}' and level='Alarm' and occur_time>'{1}' and occur_time<'{2}'",
                module, from, to);
            DataTable dataTable = QueryDataClient.Instance.Service.QueryData(sql);
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    foreach (var item in SelectedData)
                    {
                        var seriesItem = item as ChartDataLineX;
                        if (seriesItem.ProcessGuid == processguid)
                        {
                            for (int i = 0; i < dataTable.Rows.Count; i++)
                            {
                                long time = (((DateTime)dataTable.Rows[i]["occur_time"]).Ticks - from.Ticks) / 10000000;
                                seriesItem.AppedAlarm(time, dataTable.Rows[i]["description"].ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }));
        }

        public bool ParameterCheck(PMNodeInfo node)
        {
            if (Recipes.Count <= 0)
            {
                return false;
            }

            if(node.Selected)
            {
                foreach (RecipeItemSic itemSic in Recipes)
                {
                    string nodeStr = itemSic.Chamber + "." + node.NodeStr;                          //字段名称
                    string nodeStr_1 = itemSic.LotID;                                               //显示名称
                    string nodeWithGuid = node.NodeStr + "_" + itemSic.ProcessGuid;                 //字段名+ Guid (唯一ID)
                    DateTime dtStartTime = DateTime.Parse(itemSic.StartTime);
                    DateTime dtEndTime = DateTime.Parse(itemSic.EndTime);


                    lock (_lockSelection)
                    {
                        bool isExist = SelectedData.FirstOrDefault(x => (x as ChartDataLineX).ProcessGuid == nodeWithGuid) != null;
                        if (!isExist)
                        {
                            if (SelectedData.Count < MAX_PARAMETERS)
                            {
                                var line = new ChartDataLineX(nodeStr);
                                line.ProcessGuid = nodeWithGuid;
                                line.RecName = nodeStr_1;
                                line.Tag = node;
                                SelectedData.Add(line);
                                SelectedDataChanged();
                                QueryIndexer indexer = _lstTokenTimeData.FirstOrDefault(x => x.ProcessGuid == line.ProcessGuid);

                                if (indexer == null)
                                {
                                    indexer = new QueryIndexer()
                                    {
                                        Module = line.Module,
                                        StartTime = dtStartTime,
                                        EndTime = dtEndTime,
                                        ProcessGuid = line.ProcessGuid,
                                        ColumneName = nodeStr,
                                        DateList = GetDateList(dtStartTime, dtEndTime)
                                    };

                                    _lstTokenTimeData.Add(indexer);
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {
                lock (_lockSelection)
                {
                    for (int i = SelectedData.Count - 1; i >= 0; i--)
                    {
                        if ((SelectedData[i] as ChartDataLineX).DataName.Contains(node.NodeStr))
                        {
                            SelectedData.RemoveAt(i);
                        }
                    }
                }
            }
            return true;
        }       

        public void SelectColor(ChartDataLineX cp)
        {
            if (cp == null)
                return;

            var dlg = new System.Windows.Forms.ColorDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                cp.Stroke = new System.Windows.Media.Color() { A = dlg.Color.A, B = dlg.Color.B, G = dlg.Color.G, R = dlg.Color.R };
            }
        }

        public void SetYValue()
        {
            VisibleRangeYLimit = new DoubleRange(Convert.ToDouble(UseSetY1), Convert.ToDouble(UseSetY2));
        }

        public void SetXValue()
        {
            VisibleRangeXLimit = new DoubleRange(Convert.ToDouble(UseSetX1), Convert.ToDouble(UseSetX2));
        }

        /// <summary>
        /// 获取开始时间和结束时间之间的所有日期
        /// </summary>
        /// <param name="timeStart"></param>
        /// <param name="timeEnd"></param>
        /// <returns></returns>
        private List<string> GetDateList(DateTime timeStart, DateTime timeEnd)
        {
            List<string> lstTime = new List<string>();
            string dtEndStr = timeEnd.ToString("yyyyMMdd");
            int timeSpan = (int)(timeEnd - timeStart).TotalDays + 1;
            for (int i = 0; i <= timeSpan; i++)
            {
                string dt = timeStart.AddDays(i).ToString("yyyyMMdd");
                lstTime.Add(dt);
                if (dt == dtEndStr)
                {
                    break;
                }
            }

            return lstTime;
        }

        /// <summary>
        /// 获取所有的列名
        /// </summary>
        private List<string> GetColumnList(string modleName)
        {
            List<string> _seletedItemName = new List<string>();
            foreach (PMNodeInfo node in ConfigNodes)
            {
                if (node.Selected)
                {
                    _seletedItemName.Add("\""+ modleName + "."+node.NodeStr+"\"");
                }
            }
            return _seletedItemName;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void ApplyFilter()
        {
            foreach (var node in ConfigNodes)
                node.ApplyCriteria(CurrentCriteria);
        }

        public void SetTimeSpan()
        {
            if (TrendInterval < 1000)
            {
                TrendInterval = 1000;
            }
            else if (TrendInterval > 60000)
            {
                TrendInterval = 60000;
            }
            _threadReal.ChangeInterval(TrendInterval);
        }



        public ActionCommand DefaultZoomCommand { get; private set; }
        public ActionCommand ShowLegendCommand { get; private set; }
        public ActionCommand DataDetailVisbleCommand { get; private set; }
        public ActionCommand ShowAlarmCommand { get; private set; }

        private void SetDataDetailVisble()
        {
            if (DataDetailVisbility == Visibility.Collapsed)
            {
                DataDetailVisbility = Visibility.Visible;
            }
            else
            {
                DataDetailVisbility = Visibility.Collapsed;
            }
        }

        private void SetLegendAvalible()
        {
            ShowLegendInfo = !ShowLegendInfo;
        }

        private void Zoom()
        {
            ViewportManager.AnimateZoomExtents(TimeSpan.FromMilliseconds(500));
        }

        public void DataStatistics()
        {
            foreach (var item in SelectedData)
            {
                (item as ChartDataLineX).Caculate(ThresholdX1, ThresholdX2);
            }
        }

        public void DataStatisticsReset()
        {
            ThresholdX1 = 0;
            ThresholdX2 = 1;
        }

        //public void DataStatisticsCurrent()
        //{

        //}

        public void AddRangeAnnotation()
        {
            AlarmAnnotation = new AnnotationCollection();
            VerticalLineAnnotation lineX1 = new VerticalLineAnnotation()
            {
                X1 = ThresholdX1,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                IsEditable = true,
                LabelPlacement = LabelPlacement.Axis,
                LabelTextFormatting = "0.00",
                ShowLabel = true,
                StrokeThickness = 2,
                Stroke = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
            };

            lineX1.SetBinding(VerticalLineAnnotation.X1Property, new Binding("ThresholdX1") { Mode = BindingMode.TwoWay });

            AlarmAnnotation.Add(lineX1);

            VerticalLineAnnotation lineX2 = new VerticalLineAnnotation()
            {
                X1 = ThresholdX1,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                IsEditable = true,
                LabelPlacement = LabelPlacement.Axis,
                LabelTextFormatting = "0.00",
                ShowLabel = true,
                StrokeThickness = 2,
                Stroke = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
            };
            lineX2.SetBinding(VerticalLineAnnotation.X1Property, new Binding("ThresholdX2") { Mode = BindingMode.TwoWay });
            AlarmAnnotation.Add(lineX2);
        }

        private List<string> lstAlarmAdded = new List<string>();
        private bool _showAlarm = false;

        public void AddAlarmInfo()
        {
            lstAlarmAdded = new List<string>();
            while (AlarmAnnotation.Count > 2)
            {
                AlarmAnnotation.RemoveAt(AlarmAnnotation.Count - 1);
            }

            _showAlarm = !_showAlarm;
            if (_showAlarm)
            {
                lock (_lockSelection)
                {
                    foreach (var item in SelectedData)
                    {
                        var data = item as ChartDataLineX;
                        if (data.IsVisible && data.ProcessGuid.Split('_').Length == 2 && !lstAlarmAdded.Contains(data.ProcessGuid.Split('_')[1]))
                        {
                            lstAlarmAdded.Add(data.ProcessGuid.Split('_')[1]);//同一个Guid不重复添加

                            for (int i = 0; i < data._alarmData.Count; i++)
                            {
                                System.Windows.Media.DoubleCollection dc = new System.Windows.Media.DoubleCollection();
                                dc.Add(5);
                                dc.Add(5);

                                AnnotationLabel lb = new AnnotationLabel() { LabelPlacement = LabelPlacement.TopRight, Text = "Alarm" + data._alarmData[i].Item2 };
                                VerticalLineAnnotation alarm1 = new VerticalLineAnnotation()
                                {
                                    X1 = data._alarmData[i].Item1,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    FontSize = 13,
                                    FontWeight = FontWeights.Bold,
                                    IsEditable = true,
                                    LabelPlacement = LabelPlacement.Axis,
                                    StrokeThickness = 1,
                                    Stroke = System.Windows.Media.Brushes.Brown,
                                    Foreground = System.Windows.Media.Brushes.White,
                                    StrokeDashArray = dc,
                                    AnnotationLabels = new ObservableCollection<AnnotationLabel>(new List<AnnotationLabel>() { lb }),
                                };
                                alarm1.IsEnabled = false;
                                AlarmAnnotation.Add(alarm1);
                            }

                        }
                    }
                }
            }
        }

    }
}
