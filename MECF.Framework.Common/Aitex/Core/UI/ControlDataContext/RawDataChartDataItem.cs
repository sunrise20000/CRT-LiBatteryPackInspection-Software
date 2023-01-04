using System;
using System.ComponentModel;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Data.Model;

namespace Aitex.Core.UI.ControlDataContext
{
	public class RawDataChartDataItem : INotifyPropertyChanged
	{
		private AutoRange _AutoRange;

		private DoubleRange _visibleRange;

		private bool _temperatureVisible = true;

		private bool _reflectVisible = true;

		private bool _curvatureVisible = true;

		private IDataSeries _reflectData;

		private IDataSeries _temperatureData;

		private IDataSeries _curvatureData;

		public AutoRange AutoRange
		{
			get
			{
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				return _AutoRange;
			}
			set
			{
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				//IL_0003: Unknown result type (might be due to invalid IL or missing references)
				_AutoRange = value;
				InvokePropertyChanged("AutoRange");
			}
		}

		public DoubleRange VisibleRange
		{
			get
			{
				return _visibleRange;
			}
			set
			{
				_visibleRange = value;
				InvokePropertyChanged("VisibleRange");
			}
		}

		public bool TemperatureVisible
		{
			get
			{
				return _temperatureVisible;
			}
			set
			{
				_temperatureVisible = value;
				InvokePropertyChanged("TemperatureVisible");
			}
		}

		public bool ReflectVisible
		{
			get
			{
				return _reflectVisible;
			}
			set
			{
				_reflectVisible = value;
				InvokePropertyChanged("ReflectVisible");
			}
		}

		public bool CurvatureVisible
		{
			get
			{
				return _curvatureVisible;
			}
			set
			{
				_curvatureVisible = value;
				InvokePropertyChanged("CurvatureVisible");
			}
		}

		public IDataSeries ReflectData
		{
			get
			{
				return _reflectData;
			}
			set
			{
				_reflectData = value;
				InvokePropertyChanged("ReflectData");
			}
		}

		public IDataSeries TemperatureData
		{
			get
			{
				return _temperatureData;
			}
			set
			{
				_temperatureData = value;
				InvokePropertyChanged("TemperatureData");
			}
		}

		public IDataSeries CurvatureData
		{
			get
			{
				return _curvatureData;
			}
			set
			{
				_curvatureData = value;
				InvokePropertyChanged("CurvatureData");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public RawDataChartDataItem()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			AutoRange = (AutoRange)1;
			VisibleRange = new DoubleRange(0.0, 10.0);
			_temperatureData = (IDataSeries)(object)GetSeries();
			_reflectData = (IDataSeries)(object)GetSeries();
			_curvatureData = (IDataSeries)(object)GetSeries();
		}

		private XyDataSeries<double, double> GetSeries()
		{
			Random random = new Random();
			XyDataSeries<double, double> val = new XyDataSeries<double, double>();
			for (int i = 0; i < 1000; i++)
			{
				val.Append((double)(i + 1), (double)(i + random.Next(0, 100)));
			}
			return val;
		}
	}
}
