using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using Aitex.Core.Util;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Framework;

namespace MECF.Framework.Common.ControlDataContext
{
	public class ChartDataLine : FastLineRenderableSeries, INotifyPropertyChanged
	{
		private double _dataFactor = 1.0;

		private double _dataOffset = 0.0;

		private int _capacity = 10000;

		private object _lockData = new object();

		private FixSizeQueue<Tuple<DateTime, double>> _queueRawData;

		public string UniqueId { get; set; }

		public List<Tuple<DateTime, double>> Points
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
			get { return DataSeries.SeriesName; }
			set { DataSeries.SeriesName = value; }
		}

		public double LineThickness
		{
			get { return StrokeThickness; }
			set
			{
				int num = Convert.ToInt32(value);
				if (num < 1)
				{
					num = 1;
				}

				if (num > 100)
				{
					num = 100;
				}

				StrokeThickness = num;
				InvokePropertyChanged("LineThickness");
			}
		}

		public double DataFactor
		{
			get { return _dataFactor; }
			set
			{
				if (Math.Abs(_dataFactor - value) > 0.001)
				{
					_dataFactor = value;
					InvokePropertyChanged("DataFactor");
					UpdateChartSeriesValue();
				}
			}
		}

		public double DataOffset
		{
			get { return _dataOffset; }
			set
			{
				if (Math.Abs(_dataOffset - value) > 0.001)
				{
					_dataOffset = value;
					InvokePropertyChanged("DataFactor");
					UpdateChartSeriesValue();
				}
			}
		}

		public int Capacity
		{
			get { return _capacity; }
			set
			{
				_capacity = value;
				if (_queueRawData != null)
				{
					_queueRawData.FixedSize = value;
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
			PropertyChanged?.Invoke(this, e);
		}

		public void InvokePropertyChanged()
		{
			Type type = GetType();
			PropertyInfo[] properties = type.GetProperties();
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				InvokePropertyChanged(propertyInfo.Name);
			}
		}

		public ChartDataLine(string dataName)
			: this(dataName, dataName, Colors.Blue)
		{
		}

		public ChartDataLine(string dataName, string displayName, Color seriesColor)
		{
			UniqueId = Guid.NewGuid().ToString();
			_queueRawData = new FixSizeQueue<Tuple<DateTime, double>>(_capacity);
			XAxisId = "DefaultAxisId";
			YAxisId = "DefaultAxisId";
			DataSeries = new XyDataSeries<DateTime, double>(0);
			DisplayName = displayName;
			DataName = dataName;
			Stroke = seriesColor;
			IsVisible = true;
			DataOffset = 0.0;
			LineThickness = 1.0;
			DataFactor = 1.0;
		}

		public void Append(DateTime dt, double value)
		{
			lock (_lockData)
			{
				_queueRawData.Enqueue(Tuple.Create(dt, value));
				if (DataSeries is XyDataSeries<DateTime, double> val)
				{
					val.Append(dt, value * _dataFactor + _dataOffset);
					while (val.Count > _capacity)
					{
						val.RemoveRange(0, val.Count - _capacity);
					}
				}
			}
		}

		public void UpdateChartSeriesValue()
		{
			lock (_lockData)
			{
				IUpdateSuspender val = DataSeries.SuspendUpdates();
				try
				{
					XyDataSeries<DateTime, double> val2 = DataSeries as XyDataSeries<DateTime, double>;
					for (int i = 0; i < val2.Count; i++)
					{
						val2.Update(i, _queueRawData.ElementAt(i).Item2 * _dataFactor + _dataOffset);
					}
				}
				finally
				{
					val?.Dispose();
				}
			}
		}

		public void ClearData()
		{
			lock (_lockData)
			{
				_queueRawData.Clear();
				(DataSeries as XyDataSeries<DateTime, double>)?.Clear();
			}
		}
	}
}
