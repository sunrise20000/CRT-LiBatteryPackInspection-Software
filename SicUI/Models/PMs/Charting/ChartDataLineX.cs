using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using Aitex.Core.Util;
using System.Windows.Media;
using System.Threading.Tasks;
using SciChart.Charting.Visuals.Annotations;
using System.Windows;
using System.Collections.ObjectModel;

namespace SicUI.Models.PMs.Charting
{
    public class ChartDataLineX : FastLineRenderableSeries, INotifyPropertyChanged
    {

        private string[] _dbModules = { "PM1", "PM2" };

        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void InvokePropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs eventArgs = new PropertyChangedEventArgs(propertyName);
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed != null)
            {
                changed(this, eventArgs);
            }
        }
        public void InvokePropertyChanged()
        {
            Type t = this.GetType();
            var ps = t.GetProperties();
            foreach (var p in ps)
            {
                InvokePropertyChanged(p.Name);
            }
        }
        #endregion

        public string UniqueId { get; set; }

        public string ProcessGuid { get; set; }

        public string Module { get; set; }
        public string RecName { get; set; }

        public List<Tuple<double, double>> Points
        {
            get
            {
                lock (_lockData)
                {
                    return _queueRawData.ToList();
                }
            }
        }

        public string DataSource { get; set; }
        public string DataName { get; set; }

        public string DisplayName
        {
            get
            {
                return DataSeries.SeriesName;
            }
            set
            {
                DataSeries.SeriesName = value;
            }
        }
        public double LineThickness
        {
            get
            {
                return StrokeThickness;
            }
            set
            {
                var i = Convert.ToInt32(value);
                if (i < 1) i = 1;
                if (i > 100) i = 100;
                StrokeThickness = i;
                InvokePropertyChanged("LineThickness");
            }
        }

        private double _dataFactor = 1.0;
        public double DataFactor
        {
            get => _dataFactor;
            set
            {
                if (Math.Abs(_dataFactor - value) > 0.001)
                {
                    _dataFactor = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }

        private double _dataOffset = 0;
        public double DataOffset
        {
            get => _dataOffset;
            set
            {
                if (Math.Abs(_dataOffset - value) > 0.001)
                {
                    _dataOffset = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }
        private double _dataxOffset = 0;
        public double DataXOffset
        {
            get => _dataxOffset;
            set
            {
                _dataxOffset = value;
                InvokePropertyChanged(nameof(DataFactor));
                RePaintChartSeriesValue();                
            }
        }

        private int _capacity = 10000;

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
                //DataSeries.FifoCapacity = value;
                if (_queueRawData != null)
                {
                    _queueRawData.FixedSize = value;
                }

            }
        }

        private object _lockData = new object();

        private FixSizeQueue<Tuple<double, double>> _queueRawData;
        public List<Tuple<long, string>> _alarmData;

        public ChartDataLineX(string dataName)
        : this(dataName, dataName, Colors.Blue)
        {
            Module = "System";
            foreach (var dbModule in _dbModules)
            {
                if (dataName.StartsWith(dbModule))
                {
                    Module = dbModule;
                    break;
                }
            }
        }

        public ChartDataLineX(string dataName, string displayName, Color seriesColor)
        {
            UniqueId = Guid.NewGuid().ToString();

            _queueRawData = new FixSizeQueue<Tuple<double, double>>(_capacity);
            _alarmData = new List<Tuple<long, string>>();

            XAxisId = "DefaultAxisId";
            YAxisId = "DefaultAxisId";
            DataSeries = new XyDataSeries<double, double>(0);

            DisplayName = displayName;

            DataName = dataName;

            Stroke = seriesColor;

            IsVisible = true;
            DataOffset = 0;
            LineThickness = 1;
            DataFactor = 1;
        }

        public void Append(double xvalue, double value)
        {
            lock (_lockData)
            {
                _queueRawData.Enqueue(Tuple.Create(xvalue, value));
                var series = DataSeries as XyDataSeries<double, double>;
                if (series != null)
                {
                    series.Append(xvalue, value * _dataFactor + _dataOffset);
                    //while (series.Count > _capacity)
                    //{
                    //    series.RemoveRange(0, series.Count - _capacity);
                    //}
                }
            }
        }

        public void UpdateChartSeriesValue()
        {
            lock (_lockData)
            {
                using (DataSeries.SuspendUpdates())
                {
                    XyDataSeries<double, double> s = DataSeries as XyDataSeries<double, double>;
                    for (int i = 0; i < s.Count; i++)
                    {
                        s.Update(i, _queueRawData.ElementAt(i).Item2 * _dataFactor + _dataOffset);
                    }
                }
            }
        }

        public void RePaintChartSeriesValue()
        {
            lock (_lockData)
            {
                using (DataSeries.SuspendUpdates())
                {
                    DataSeries.Clear();
                    var series = DataSeries as XyDataSeries<double, double>;
                    for (int i = 0; i < _queueRawData.Count; i++)
                    {
                        series.Append(_queueRawData.ElementAt(i).Item1 + DataXOffset, _queueRawData.ElementAt(i).Item2 * _dataFactor + _dataOffset);
                    }
                }
            }
        }


        public void ClearData()
        {
            lock (_lockData)
            {
                _queueRawData.Clear();
                (DataSeries as XyDataSeries<double, double>)?.Clear();
            }
        }

        public void AppedAlarm(long timeTicks, string alarmID)
        {
            _alarmData.Add(Tuple.Create<long, string>(timeTicks, alarmID));
        }



        private int _pointCount;
        public int PointCount
        {
            get { return _pointCount; }
            set
            {
                _pointCount = value;
                InvokePropertyChanged(nameof(PointCount));
            }
        }

        private double _variance;
        public double Variance
        {
            get { return _variance; }
            set
            {
                _variance = value;
                InvokePropertyChanged(nameof(Variance));
            }
        }

        private double _maxValue;
        public double MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                InvokePropertyChanged(nameof(MaxValue));
            }
        }
        private double _minValue;
        public double MinValue
        {
            get { return _minValue; }
            set
            {
                _minValue = value;
                InvokePropertyChanged(nameof(MinValue));
            }
        }

        private double _avgValue;
        public double AvgValue
        {
            get { return _avgValue; }
            set
            {
                _avgValue = value;
                InvokePropertyChanged(nameof(AvgValue));
            }
        }

        

        public void Caculate(double x1,double x2)
        {
            //if (RecName == "RealTime")
            //{
            //    return;
            //}

            double min = x1;
            double max = x2;
            if (x1 > x2)
            {
                min = x2;
                max = x1;
            }
            if (max - min < 10)
            {
                min = 0;
                max = 100000000;
            }

            PointCount = Points.Where(a => a.Item1 >= min && a.Item1 <= max).Count();
            if (PointCount == 0)
            {
                MaxValue = 0;
                MinValue = 0;
                AvgValue = 0;
                Variance = 0;
            }
            else
            {
                MaxValue = Points.Where(a => a.Item1 >= min && a.Item1 <= max).Max(x => x.Item2);
                MinValue = Points.Where(a => a.Item1 >= min && a.Item1 <= max).Min(x => x.Item2);
                AvgValue = Points.Where(a => a.Item1 >= min && a.Item1 <= max).Average(x => x.Item2);
                Variance = Points.Where(a => a.Item1 >= min && a.Item1 <= max).Sum(x => Math.Pow(x.Item2 - AvgValue, 2)) / PointCount;
            }
        }

    }
}
