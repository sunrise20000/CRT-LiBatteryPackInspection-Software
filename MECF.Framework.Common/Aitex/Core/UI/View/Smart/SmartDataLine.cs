using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using Aitex.Core.Util;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;

namespace Aitex.Core.UI.View.Smart
{
	public class SmartDataLine : FastLineRenderableSeries, INotifyPropertyChanged
	{
		public DateTime NextQueryTime { get; set; }

		public bool IsDefaultVisable { get; set; }

		public Color DefaultSeriesColor { get; set; }

		public string DbDataName { get; private set; }

		public string DisplayName
		{
			get
			{
				return DataSeries.SeriesName;
			}
			set
			{
				DataSeries.SeriesName=(value);
				InvokePropertyChanged("DisplayName");
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
				int num = Convert.ToInt32(value);
				if (num < 1)
				{
					num = 1;
				}
				if (num > 100)
				{
					num = 100;
				}
				StrokeThickness=(num);
				InvokePropertyChanged("LineThickness");
			}
		}

		public string UniqueId { get; private set; }

		public DataItem Points { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public SmartDataLine(string displayName, Color seriesColor, string dbName, bool isVisable)
		{
			UniqueId = Guid.NewGuid().ToString();
			XAxisId=("DefaultAxisId");
			YAxisId=("DefaultAxisId");
			DataSeries=new XyDataSeries<DateTime, float>();
			DisplayName = displayName;
			DbDataName = dbName;
			Stroke=(seriesColor);
			DefaultSeriesColor = seriesColor;
			NextQueryTime = DateTime.MinValue;
			IsVisible=(isVisable);
			IsDefaultVisable = isVisable;
		}

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
	}
}
