using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Core;
using MECF.Framework.UI.Client.CenterViews.Core.Charting;
using MECF.Framework.UI.Client.CenterViews.Core.EventArgs;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using Sicentury.Core.EventArgs;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.Operations.RealTime
{
    public class RealtimeViewModel : BusyIndicateableUiViewModelBase
    {
        #region Variables

        /// <summary>
        /// 允许监控的最大曲线数量
        /// </summary>
        private const int MAX_PARAMETERS_ALLOWED = 200;

        private readonly RealtimeProvider _provider = new RealtimeProvider();

        private bool _enableAutoZoom = true;
        private int _pointCount;
        private readonly PeriodicJob _thread;

        /// <summary>
        /// Monitor中添加数据点时直接向DataSeries对象添加，已实现跨线程操作。
        /// </summary>
        private readonly List<SicFastLineDataSeries> _dataSeriesList;

        /// <summary>
        /// 更新报告导出信息。
        /// </summary>
        private readonly IProgress<ProgressUpdatingEventArgs> _exportingProgressReporter;

        #endregion

        #region Constructors

        public RealtimeViewModel()
        {
            DisplayName = "Realtime";

            SelectedData = new ChartingLineSeriesCollection(DisplayName);

            ParameterNodes = new TreeNode(DisplayName)
            {
                MaxTerminalSelectionAllowed = MAX_PARAMETERS_ALLOWED
            };

            ParameterNodes.ChildNodes.AddRange(_provider.GetParameters());
            ParameterNodes.TerminalNodeSelectionChanged += OnNodeSelectionChanged;

            IntervalSaved = true;
            TrendInterval = 500;
            TimeSpanSaved = true;
            TrendTimeSpan = 60 * 5;

            _thread = new PeriodicJob(TrendInterval, MonitorData, "RealTime", true);
            _pointCount = Math.Max(TrendTimeSpan * 1000 / TrendInterval, 10);

            _dataSeriesList = new List<SicFastLineDataSeries>();

            _exportingProgressReporter = new Progress<ProgressUpdatingEventArgs>(e =>
            {
                BusyIndicatorContent = e.Message;
            });
        }

        #endregion

        #region Properties

        public bool IsPermission => this.Permission == 3;

        public TreeNode ParameterNodes { get; }

        public ChartingLineSeriesCollection SelectedData { get; set; }


        public AutoRange ChartAutoRange => EnableAutoZoom ? AutoRange.Always : AutoRange.Never;

        public bool EnableAutoZoom
        {
            get => _enableAutoZoom;

            set
            {
                _enableAutoZoom = value;
                NotifyOfPropertyChange(nameof(EnableAutoZoom));
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }

        [IgnorePropertyChange]
        public int TrendInterval { get; set; }

        public bool IntervalSaved { get; set; }

        [IgnorePropertyChange]
        public int TrendTimeSpan { get; set; }

        public bool TimeSpanSaved { get; set; }

        #endregion

        #region Methods

        protected bool MonitorData()
        {
            try
            {
                Dictionary<string, object> data = null;
                if (SelectedData.Count > 0)
                {
                    data = QueryDataClient.Instance.Service.PollData(Array.ConvertAll(SelectedData.ToArray(), x => (x as SicFastLineSeries).DataName));
                }

                AppendData(data);

                for (var j = 0; j < ParameterNodes.ChildNodes.Count; j++)
                {
                    var par = ParameterNodes.ChildNodes[j];
                    par.IsVisibilityParentNode = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }

        public void AppendData(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            var dt = DateTime.Now;

            foreach (var ds in _dataSeriesList)
            {
                if (ds == null || !data.ContainsKey(ds.SeriesName))
                    continue;

                if (data[ds.SeriesName] is bool)
                {
                    ds.Append(dt, ((bool)data[ds.SeriesName] ? 1 : 0) * ds.Factor + ds.Offset, new ParameterNodePoint(dt, ((bool)data[ds.SeriesName] ? 1 : 0)));
                    continue;
                }

                if (!double.TryParse(data[ds.SeriesName].ToString(), out var value))
                    continue;

                ds.Append(dt, value * ds.Factor + ds.Offset, new ParameterNodePoint(dt, value));
            }
        }

        private void UpdateFifoCapacity(int capacity)
        {
            foreach (var renderableSeries in SelectedData)
            {
                if (renderableSeries is SicFastLineSeries line)
                {
                    var ds = line.GetDataSeries();
                    var copyMetaData = ds.Metadata.Where(x => x != null && ((ParameterNodePoint)x).Time > DateTime.Now.AddSeconds(-TrendTimeSpan)).ToList();

                    ds.FifoCapacity = capacity;
                    foreach (ParameterNodePoint item in copyMetaData)
                    {
                        ds.Append(item.Time, item.Value);
                    }
                }
            }
        }

        public void SetInterval()
        {
            _thread.ChangeInterval(TrendInterval);
            _pointCount = Math.Max(10, TrendTimeSpan * 1000 / TrendInterval);
            IntervalSaved = true;
            NotifyOfPropertyChange(nameof(IntervalSaved));

            UpdateFifoCapacity(_pointCount);
        }

        public void SetTimeSpan()
        {
            _pointCount = Math.Max(10, TrendTimeSpan * 1000 / TrendInterval);
            TimeSpanSaved = true;
            NotifyOfPropertyChange(nameof(TimeSpanSaved));

            UpdateFifoCapacity(_pointCount);
        }

        #endregion

        #region Events

        public void OnNodeSelectionChanged(object sender, TreeNodeSelectionChangedEventArgs e)
        {
            if (e.NewValue == true && SelectedData.Count >= MAX_PARAMETERS_ALLOWED)
            {
                e.Cancel = true;
                return;
            }

            var line = SelectedData.Cast<SicFastLineSeries>().FirstOrDefault(x => x.DataName == e.Source.FullName);
            switch (e.NewValue)
            {
                case false:
                    {
                        if (line != null)
                        {
                            _dataSeriesList.Remove(line.GetDataSeries());
                            SelectedData.Remove(line);
                        }

                        break;
                    }

                case true:
                    {
                        if (line == null)
                        {
                            var series = new SicFastLineSeries(e.Source.FullName)
                            {
                                BackendParameterNode = e.Source
                            };
                            series.GetDataSeries().FifoCapacity = _pointCount;

                            SelectedData.Add(series);
                            _dataSeriesList.Add(series.GetDataSeries());
                            SelectedData.ResetColors();
                        }

                        break;
                    }

                default:
                    break;
            }
        }

        public void Deleted(object sender, RenderableSeriesDeletingEventArgs e)
        {
            var list = e.Source.Cast<SicFastLineSeries>().ToList();

            var total = SelectedData.Count;
            for (var i = total - 1; i >= 0; i--)
            {
                list[i].BackendParameterNode.IsSelected = false;
            }

            _dataSeriesList.Clear();
            ((RealtimeView)View).tvParameterNodes.ClearPresetGroupSelectionOnly();
        }

        public void Exporting(object sender, EventArgs e)
        {
            BusyIndicatorContent = "Exporting ...";
            IsBusy = true;
        }

        public void Exported(object sender, EventArgs e)
        {
            IsBusy = false;
        }

        public void ProgressUpdating(object sender, ProgressUpdatingEventArgs e)
        {
            _exportingProgressReporter.Report(e);
        }

        public override void Cancel()
        {
            if (View is RealtimeView view)
            {
                view.dataGrid.CancelOperation();
            }
        }

        #endregion
    }
}
