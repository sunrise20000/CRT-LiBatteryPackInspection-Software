/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\Core\UserControls\DataViewDataGrid.cs
* @author Su Liang
* @Date 2022-08-01
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details
* *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MECF.Framework.UI.Client.CenterViews.Core.Charting;
using MECF.Framework.UI.Client.CenterViews.Core.EventArgs;
using SciChart.Charting.Visuals.RenderableSeries;
using Sicentury.Core.EventArgs;
using MessageBox = System.Windows.MessageBox;

namespace MECF.Framework.UI.Client.CenterViews.Core.UserControls
{
    /// <summary>
    /// Interaction logic for DataViewDataGrid.xaml
    /// </summary>
    public partial class DataViewDataGrid
    {

        #region Variables

        /// <summary>
        /// 正在导出数据事件。
        /// </summary>
        public event EventHandler Exporting;

        /// <summary>
        /// 导出数据完毕事件。
        /// <para>导出过程异常仍触发此事件。</para>
        /// </summary>
        public event EventHandler Exported;

        /// <summary>
        /// 正在删除曲线事件。
        /// </summary>
        public event EventHandler<RenderableSeriesDeletingEventArgs> Deleting;

        /// <summary>
        /// 删除曲线完成事件。
        /// </summary>
        public event EventHandler<RenderableSeriesDeletingEventArgs> Deleted;

        /// <summary>
        /// 常时操作的进度信息更新事件。
        /// </summary>
        public event EventHandler<ProgressUpdatingEventArgs> ProgressMessageUpdating;

        /// <summary>
        /// 取消操作。
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 在UI线程上报告进度。
        /// </summary>
        private readonly IProgress<ProgressUpdatingEventArgs> _progress;

        #endregion

        #region Constructors

        public DataViewDataGrid()
        {
            _progress = new Progress<ProgressUpdatingEventArgs>(e => { ProgressMessageUpdating?.Invoke(this, e); });
        }

        private void ItemsSourceOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    RegisterPropertiesChangedEvent(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RegisterPropertiesChangedEvent(e.NewItems);
                    UnRegisterPropertiesChangedEvent(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    UnRegisterPropertiesChangedEvent(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    UnRegisterPropertiesChangedEvent(e.OldItems);
                    RegisterPropertiesChangedEvent(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Move:
                default:
                    // Ignore.
                    break;
            }
        }

        private void RegisterPropertiesChangedEvent(IList collection)
        {
            foreach (var item in collection)
            {
                if (item is SicFastLineSeries series)
                {
                    series.PropertyChanged += SeriesOnPropertyChanged;
                }
            }
        }

        private void UnRegisterPropertiesChangedEvent(IList collection)
        {
            foreach (var item in collection)
            {
                if (item is SicFastLineSeries series)
                {
                    series.PropertyChanged -= SeriesOnPropertyChanged;
                }
            }
        }

        private void SeriesOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRenderableSeries.IsVisible))
            {
                // 设置标题栏中CheckBox的IsChecked属性
                var group =
                    ItemsSource.ToList()
                        .Cast<SicFastLineSeries>()
                        .GroupBy(x => x.IsVisible)
                        .ToList();

                if (group.Count() > 1)
                {
                    IsVisibleInColumnHeader = null;
                }
                else if (group.Count() == 1)
                {
                    var visible = group[0].Key;
                    IsVisibleInColumnHeader = visible;
                }
            }
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(ChartingLineSeriesCollection), typeof(DataViewDataGrid),
            new PropertyMetadata(default(ChartingLineSeriesCollection), (sender, e) =>
            {
                //if (!(sender is DataViewDataGrid dg))
                //    return;

                //if (e.NewValue is ChartingLineSeriesCollection newColl)
                //{
                //    newColl.CollectionChanged += dg.ItemsSourceOnCollectionChanged;
                //    dg.RegisterPropertiesChangedEvent(newColl);
                //}

                //if (e.OldValue is ChartingLineSeriesCollection oldColl)
                //{
                //   oldColl.CollectionChanged -= dg.ItemsSourceOnCollectionChanged;
                //   dg.UnRegisterPropertiesChangedEvent(oldColl);
                //}

            }));

        /// <summary>
        /// 设置或返回DataGrid数据源。
        /// </summary>
        public ChartingLineSeriesCollection ItemsSource
        {
            get => (ChartingLineSeriesCollection)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }


        public static readonly DependencyProperty IsShowStatisticColumnProperty = DependencyProperty.Register(
            nameof(IsShowStatisticColumn),
            typeof(bool),
            typeof(DataViewDataGrid),
            new PropertyMetadata(default(bool)));

        /// <summary>
        /// 设置或返回是否显示统计数据列。
        /// </summary>
        public bool IsShowStatisticColumn
        {
            get => (bool)GetValue(IsShowStatisticColumnProperty);
            set => SetValue(IsShowStatisticColumnProperty, value);
        }

        public static readonly DependencyProperty IsVisibleInColumnHeaderProperty = DependencyProperty.Register(
            "IsVisibleInColumnHeader", 
            typeof(bool?), 
            typeof(DataViewDataGrid), 
            new PropertyMetadata(default(bool?), (sender, e) =>
            {
                if (!(sender is DataViewDataGrid dg))
                    return;

                if (e.Property.Name != nameof(IsVisibleInColumnHeader))
                    return;

                if (!(e.NewValue is bool visible))
                    return;

                // 如果CheckBox被选中状态，则本次点击后隐藏所有序列。
             
                foreach (var series in dg.ItemsSource)
                {
                    series.IsVisible = visible;
                }
            }));

        public bool? IsVisibleInColumnHeader
        {
            get => (bool?)GetValue(IsVisibleInColumnHeaderProperty);
            set => SetValue(IsVisibleInColumnHeaderProperty, value);
        }

        #endregion

        #region Methods

        protected override void OnInitialized(System.EventArgs e)
        {
            InitializeComponent();
            base.OnInitialized(e);
        }

        /// <summary>
        /// 取消操作。
        /// </summary>
        public void CancelOperation()
        {
            if (_cancellationTokenSource?.Token.CanBeCanceled == true)
                _cancellationTokenSource.Cancel();
        }

        private void OnProgressMessageUpdating(int currentProgress, int totalProgress, string message)
        {
            _progress.Report(new ProgressUpdatingEventArgs(currentProgress, totalProgress, message));
        }

        #endregion

        #region Events

        /// <summary>
        /// 导出全部曲线数据。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnExportAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(ItemsSource is ChartingLineSeriesCollection collection))
                return;

            try
            {
                if (collection.Count == 0)
                {
                    MessageBox.Show($"Please select the data you want to export.", "Export", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

#if EXPORT_TO_CSV
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".xlsx", // Default file extension 
                    Filter = "Excel数据表格文件(*.csv)|*.csv", // Filter files by extension 
                    FileName = $"{collection.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };
#else
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".xlsx", // Default file extension 
                    Filter = "Excel数据表格文件(*.xlsx)|*.xlsx", // Filter files by extension 
                    FileName = $"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

#endif

                var ret = dlg.ShowDialog(); // Show open file dialog box
                if (ret == DialogResult.OK) // Process open file dialog box results
                {
                    Exporting?.Invoke(this, System.EventArgs.Empty);
                    _cancellationTokenSource = new CancellationTokenSource();

                    var sw = new Stopwatch();
                    sw.Restart();

#if EXPORT_TO_CSV
                    var columns = new List<string>();
#else
                    var ds = new DataSet();
                    ds.Tables.Add(new DataTable(dlg.FileName));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);
#endif

                    var timeValue = new Dictionary<DateTime, double[]>();
                    var dataSeriesCollection =
                        collection.Cast<SicFastLineSeries>().Select(x => x.GetDataSeries()).ToList();

                    await Task.Run(() =>
                    {
                        for (var i = 0; i < dataSeriesCollection.Count; i++)
                        {

                            OnProgressMessageUpdating(
                                50,
                                100,
                                $"Exporting {dataSeriesCollection[i].SeriesName} {i}/{dataSeriesCollection.Count} ...");

                            var points = dataSeriesCollection[i].Metadata
                                .Cast<ParameterNodePoint>().ToList();

                            for (var n = 0; n < points.Count; n++)
                            {
                                var p = points[n];

                                if (!timeValue.ContainsKey(p.Time))
                                    timeValue[p.Time] = new double[collection.Count];

                                timeValue[p.Time][i] = p.Value;

                                if (_cancellationTokenSource.Token.IsCancellationRequested)
                                    break;
                            }

#if EXPORT_TO_CSV
                            columns.Add((collection[i] as SicFastLineSeries)?.DataName ?? "Unknown Col");
#else
                            ds.Tables[0].Columns.Add((SelectedData[i] as SicFastLineSeries)?.DataName);
                            ds.Tables[0].Columns[i + 1].DataType = typeof(double);
#endif
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                                break;
                        }

                    }, _cancellationTokenSource.Token).ContinueWith(t =>
                    {
                        if (t.IsCanceled || t.IsFaulted)
                            return;
#if EXPORT_TO_CSV
                            var csvBuilder = new StringBuilder();
                            csvBuilder.Append("Time,");
                            csvBuilder.AppendLine(string.Join(",", columns));

                            var totalPoints = (double)timeValue.Count;
                            var processedPoints = 0d;
                            var lastPercent = 0d;
                            foreach (var tv in timeValue)
                            {
                                csvBuilder.Append($" {tv.Key:yyyy/MM/dd HH:mm:ss.fff}");
                                csvBuilder.Append(",");
                                csvBuilder.AppendLine(string.Join(",", tv.Value));

                                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                                    return;

                                processedPoints++;
                                var currentPercent = (int)(processedPoints / totalPoints * 100);
                                if (currentPercent > lastPercent)
                                {
                                    /*
                                     * 不要太频繁更新进度，否则会导致UI卡顿，每增长1%更新一次进度。
                                     * currentPercent和lastPercent必须是整数，以将进度更新的频率控制在要求内。
                                     */
                                    OnProgressMessageUpdating(
                                        60,
                                        100,
                                        $"Building report content ({currentPercent}%) ...");
                                    lastPercent = currentPercent;
                                }
                            }

                            OnProgressMessageUpdating(
                                90,
                                100,
                                $"Writing to file ...");
                            using (_cancellationTokenSource.Token.Register(Thread.CurrentThread.Abort))
                            {
                                File.WriteAllText(dlg.FileName, csvBuilder.ToString());
                            }
#else

                             OnProgressMessageUpdating(
                                60,
                                100,
                                $"Building report content ...");

                            foreach (var item in timeValue)
                            {
                                var row = ds.Tables[0].NewRow();
                                row[0] = item.Key;
                                for (var j = 0; j < item.Value.Length; j++)
                                {
                                    row[j + 1] = item.Value[j];
                                }

                                ds.Tables[0].Rows.Add(row);
                            }


                            OnProgressMessageUpdating(
                                90,
                                100,
                                $"Writing to file ...");
                                using (_cancellationTokenSource.Token.Register(Thread.CurrentThread.Abort))
                            {
                            if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out var reason))
                            {
                                MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }
                        }
#endif
                    });


                    sw.Stop();
                    Debug.WriteLine($"Export costs {sw.ElapsedMilliseconds}ms");
                    Debug.WriteLine($"Total Lines {timeValue.Count}");

                    if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                        return;

                    Exported?.Invoke(this, System.EventArgs.Empty);

                    MessageBox.Show($"Exporting succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (ThreadAbortException)
            {
                // 操作取消。
            }
            catch (AggregateException ae)
            {
                var ex = ae.Flatten().InnerExceptions;
                throw ex.FirstOrDefault() ?? ae;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to export data, {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Exported?.Invoke(this, System.EventArgs.Empty);
            }
        }

        /// <summary>
        /// 导出单条曲线数据。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnExport_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!(ItemsSource is ChartingLineSeriesCollection collection))
                return;

            if (!(sender is TextBlock btn))
                return;

            if (!(btn.DataContext is SicFastLineSeries series))
                return;

            try
            {

#if EXPORT_TO_CSV
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".xlsx", // Default file extension 
                    Filter = "Excel数据表格文件(*.csv)|*.csv", // Filter files by extension 
                    FileName = $"{series.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };
#else
                var dlg = new SaveFileDialog
                {
                    DefaultExt = ".xlsx", // Default file extension 
                    Filter = "Excel数据表格文件(*.xlsx)|*.xlsx", // Filter files by extension 
                    FileName = $"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

#endif
                var result = dlg.ShowDialog(); // Show open file dialog box
                if (result == DialogResult.OK) // Process open file dialog box results
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    Exporting?.Invoke(this, System.EventArgs.Empty);

                    var sw = new Stopwatch();
                    sw.Restart();

#if EXPORT_TO_CSV
                    var columns = new List<string>();
#else
                    var ds = new DataSet();
                    ds.Tables.Add(new DataTable(cp.DataName));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);
                    ds.Tables[0].Columns.Add(cp.DataName);
                    ds.Tables[0].Columns[1].DataType = typeof(double);
#endif

                    var ds = series?.GetDataSeries();
                    var points = ds?.Metadata.Cast<ParameterNodePoint>().ToList();

                    if (points == null)
                    {
                        MessageBox.Show($"Unable to find meta points from the series {series.DataName}.", "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    var csvBuilder = new StringBuilder();

                    await Task.Run(() =>
                    {
                        using (_cancellationTokenSource.Token.Register(Thread.CurrentThread.Abort))
                        {
#if EXPORT_TO_CSV
                            csvBuilder.AppendLine($"Time,{ds.SeriesName}"); // table header
#endif
                            OnProgressMessageUpdating(
                                50,
                                100,
                                $"Exporting data ...");

                            for (var i = 0; i < points.Count; i++)
                            {
                                var p = points[i];
#if EXPORT_TO_CSV
                                csvBuilder.AppendLine($" {p.Time:yyyy/MM/dd HH:mm:ss.fff},{p.Value}");
#else
                            var row = ds.Tables[0].NewRow();
                            row[0] = p.Time;
                            row[1] = p.Value;
                            ds.Tables[0].Rows.Add(row);
#endif
                            }

                            OnProgressMessageUpdating(
                                90,
                                100,
                                $"Writing to file ...");
#if EXPORT_TO_CSV

                            File.WriteAllText(dlg.FileName, csvBuilder.ToString());
#else
                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out var reason))
                    {
                        MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
#endif
                        }
                    });

                    sw.Stop();
                    Debug.WriteLine($"Export costs {sw.ElapsedMilliseconds}ms");
                    Debug.WriteLine($"Total Lines {points?.Count}");

                    Exported?.Invoke(this, System.EventArgs.Empty);

                    MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (ThreadAbortException)
            {
                // 操作取消。
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to export data, {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Exported?.Invoke(this, System.EventArgs.Empty);
            }
        }

        /// <summary>
        /// 删除全部曲线。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(ItemsSource is ChartingLineSeriesCollection collection))
                return;

            try
            {
                var list = collection.Cast<SicFastLineSeries>().ToList();
                var args = new RenderableSeriesDeletingEventArgs(list);

                Deleting?.Invoke(this, args);
                if (args.Cancel)
                    return;

                var total = collection.Count;
                for (var i = total - 1; i >= 0; i--)
                {
                    list[i].BackendParameterNode.IsSelected = false;
                }

                collection.Clear();

                Deleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"It's failed to delete all series, {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除单条曲线。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!(ItemsSource is ChartingLineSeriesCollection collection))
                return;

            if (!(sender is TextBlock btn))
                return;

            if (!(btn.DataContext is SicFastLineSeries series))
                return;

            try
            {
                var args = new RenderableSeriesDeletingEventArgs(
                    new List<SicFastLineSeries>(new[] { series }));

                Deleting?.Invoke(this, args);
                if (args.Cancel)
                    return;

                series.BackendParameterNode.IsSelected = false;
                collection.Remove(series);

                Deleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"It's failed to delete series, {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 更换曲线颜色。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnChangeSeriesColor_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var dlg = new System.Windows.Forms.ColorDialog();
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            if (!(sender is Border bdr))
                return;

            if (bdr.DataContext is SicFastLineSeries series)
                series.Stroke = new System.Windows.Media.Color()
                    { A = dlg.Color.A, B = dlg.Color.B, G = dlg.Color.G, R = dlg.Color.R };
        }

        #endregion
    }
}
