/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\DataLogs\Core\SicFastLineSeries.cs
* @author Su Liang
* @Date 2022-08-02
*
* @copyright &copy Sicentury Inc.
*
* @brief Reconstructed to support rich functions.
*
* @details Cross-Thread operation allowed.
*
* *****************************************************************************/


using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using SciChart.Charting.Visuals.RenderableSeries;
using Sicentury.Core.Tree;

namespace MECF.Framework.UI.Client.CenterViews.Core.Charting
{
    public class SicFastLineSeries : FastLineRenderableSeries, INotifyPropertyChanged
    {
        private double _dataFactor = 1.0;
        private double _dataOffset = 0.0;
        public event PropertyChangedEventHandler PropertyChanged;


        #region Constructors

        public SicFastLineSeries(string dataName)
            : this(dataName, dataName, Colors.Black)
        {
        }

        public SicFastLineSeries(string dataName, string displayName, Color seriesColor, int? fifoCapacity = null)
        {
            UniqueId = Guid.NewGuid().ToString();
            Stroke = seriesColor;
            IsVisible = true;
            LineThickness = 1.0;

            XAxisId = "DefaultAxisId";
            YAxisId = "DefaultAxisId";
            
            DataFactor = 1.0;
            DataOffset = 0.0;
            
            DataSeries = new SicFastLineDataSeries()
            {
                SeriesName = dataName,
                FifoCapacity = fifoCapacity,
                Factor = DataFactor,
                Offset = DataOffset
            };
            
            //! 注意目前的程序要求 DataSeries.SeriesName 必须等于 DataName.
            DisplayName = displayName;
            DataName = dataName;

        }


        #endregion

        #region Properties

        public string UniqueId { get; }

        public string DataName { get; }

        public string DisplayName
        {
            get => DataSeries.SeriesName;
            set => DataSeries.SeriesName = value;
        }

        public double LineThickness
        {
            get => StrokeThickness;
            set
            {
                var num = Convert.ToInt32(value);
                if (num < 1)
                    num = 1;
                if (num > 100)
                    num = 100;
                StrokeThickness = num;
                InvokePropertyChanged();
            }
        }

        public double DataFactor
        {
            get => _dataFactor;
            set
            {
                if (Math.Abs(_dataFactor - value) <= 0.001)
                    return;
                _dataFactor = value;
                GetDataSeries().Factor = _dataFactor;
                InvokePropertyChanged();
                UpdateChartSeriesValue();
            }
        }

        public double DataOffset
        {
            get => _dataOffset;
            set
            {
                if (Math.Abs(_dataOffset - value) <= 0.001)
                    return;
                _dataOffset = value;
                GetDataSeries().Offset = _dataOffset;
                InvokePropertyChanged();
                UpdateChartSeriesValue();
            }
        }

        public TreeNode BackendParameterNode { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 获取XyDataSeries类型的DataSeries对象
        /// </summary>
        /// <returns></returns>
        public SicFastLineDataSeries GetDataSeries()
        {
            return DataSeries as SicFastLineDataSeries;
        }

        /// <summary>
        /// /
        /// </summary>
        public void UpdateChartSeriesValue()
        {
            using (DataSeries.SuspendUpdates())
            {
                var ds = GetDataSeries();
                for (var i = 0; i < ds.Count; i++)
                {
                    var y = (ds.Metadata[i] as ParameterNodePoint)?.Value;
                    if (y == null)
                        continue;

                    ds.Update(i, (double)y * _dataFactor + DataOffset);
                }


                //var dataSeries = DataSeries as XyDataSeries<DateTime, double>;
                //for (var index = 0; index < dataSeries.Count; ++index)
                //    dataSeries.Update(index, _queueRawData.ElementAt(index).Item2 * _dataFactor + _dataOffset);
            }
        }

        public void ClearData()
        {
            DataSeries.Clear();
        }
        
        
        private void InvokePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return DataName;
        }

        #endregion

    }
}
