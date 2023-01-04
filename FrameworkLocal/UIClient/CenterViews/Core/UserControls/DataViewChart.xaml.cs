/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\Core\UserControls\DataViewChart.cs
* @author Su Liang
* @Date 2022-08-03
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details
* *****************************************************************************/


using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Framework;
using SciChart.Data.Model;

namespace MECF.Framework.UI.Client.CenterViews.Core.UserControls
{
    /// <summary>
    /// Interaction logic for DataViewChart.xaml
    /// </summary>
    public partial class DataViewChart : UserControl
    {
        public DataViewChart()
        {
            InitializeComponent();
           
        }

        #region Dependency Properties

        public static readonly DependencyProperty RenderableSeriesProperty = DependencyProperty.Register(
            "RenderableSeries", typeof(ObservableCollection<IRenderableSeries>), typeof(DataViewChart), new PropertyMetadata(default(ObservableCollection<IRenderableSeries>)));

        public ObservableCollection<IRenderableSeries> RenderableSeries
        {
            get => (ObservableCollection<IRenderableSeries>)GetValue(RenderableSeriesProperty);
            set => SetValue(RenderableSeriesProperty, value);
        }

        public static readonly DependencyProperty AutoRangeProperty = DependencyProperty.Register(
            "AutoRange", typeof(AutoRange), typeof(DataViewChart), new PropertyMetadata(default(AutoRange)));

        public AutoRange AutoRange
        {
            get => (AutoRange)GetValue(AutoRangeProperty);
            set => SetValue(AutoRangeProperty, value);
        }

        public static readonly DependencyProperty VisibleRangeTimeProperty = DependencyProperty.Register(
            "VisibleRangeTime", typeof(IRange), typeof(DataViewChart), new PropertyMetadata(default(IRange)));

        public IRange VisibleRangeTime
        {
            get => (IRange)GetValue(VisibleRangeTimeProperty);
            set => SetValue(VisibleRangeTimeProperty, value);
        }

        public static readonly DependencyProperty VisibleRangeValueProperty = DependencyProperty.Register(
            "VisibleRangeValue", typeof(IRange), typeof(DataViewChart), new PropertyMetadata(default(IRange)));

        public IRange VisibleRangeValue
        {
            get => (IRange)GetValue(VisibleRangeValueProperty);
            set => SetValue(VisibleRangeValueProperty, value);
        }

        #endregion

        #region Methods

        public void ZoomExtents()
        {
            sciChart.ZoomExtents();
        }

        public IUpdateSuspender SuspendSuspendUpdates()
        {
            return sciChart.SuspendUpdates();
        }

        #endregion

        #region Events

        private void BtnFixCurveToScreen_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            sciChart.ZoomExtents();
        }

        #endregion
    }
}
