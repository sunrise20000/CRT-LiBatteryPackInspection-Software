using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media;
using Aitex.Core.UI.MVVM;
using Aitex.Core.UI.View.Smart;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;

namespace Aitex.Core.UI.ControlDataContext
{
	public class ProcessDataChartDataItem : INotifyPropertyChanged
	{
		private int _capacity = 10000;

		private Queue<System.Drawing.Color> colorQueue = new Queue<System.Drawing.Color>(new System.Drawing.Color[80]
		{
			System.Drawing.Color.Aqua,
			System.Drawing.Color.Aquamarine,
			System.Drawing.Color.Bisque,
			System.Drawing.Color.Blue,
			System.Drawing.Color.Brown,
			System.Drawing.Color.BurlyWood,
			System.Drawing.Color.CadetBlue,
			System.Drawing.Color.CornflowerBlue,
			System.Drawing.Color.DarkBlue,
			System.Drawing.Color.DarkCyan,
			System.Drawing.Color.DarkGray,
			System.Drawing.Color.DarkGreen,
			System.Drawing.Color.DarkKhaki,
			System.Drawing.Color.DarkMagenta,
			System.Drawing.Color.DarkOliveGreen,
			System.Drawing.Color.DarkOrange,
			System.Drawing.Color.DarkSeaGreen,
			System.Drawing.Color.DarkSlateBlue,
			System.Drawing.Color.DarkSlateGray,
			System.Drawing.Color.DarkViolet,
			System.Drawing.Color.DeepPink,
			System.Drawing.Color.DeepSkyBlue,
			System.Drawing.Color.DimGray,
			System.Drawing.Color.DodgerBlue,
			System.Drawing.Color.ForestGreen,
			System.Drawing.Color.Gold,
			System.Drawing.Color.Gray,
			System.Drawing.Color.Green,
			System.Drawing.Color.GreenYellow,
			System.Drawing.Color.HotPink,
			System.Drawing.Color.Indigo,
			System.Drawing.Color.Khaki,
			System.Drawing.Color.LightBlue,
			System.Drawing.Color.LightCoral,
			System.Drawing.Color.LightGreen,
			System.Drawing.Color.LightPink,
			System.Drawing.Color.LightSalmon,
			System.Drawing.Color.LightSkyBlue,
			System.Drawing.Color.LightSlateGray,
			System.Drawing.Color.LightSteelBlue,
			System.Drawing.Color.LimeGreen,
			System.Drawing.Color.MediumOrchid,
			System.Drawing.Color.MediumPurple,
			System.Drawing.Color.MediumSeaGreen,
			System.Drawing.Color.MediumSlateBlue,
			System.Drawing.Color.MediumSpringGreen,
			System.Drawing.Color.MediumTurquoise,
			System.Drawing.Color.Moccasin,
			System.Drawing.Color.NavajoWhite,
			System.Drawing.Color.Olive,
			System.Drawing.Color.OliveDrab,
			System.Drawing.Color.Orange,
			System.Drawing.Color.OrangeRed,
			System.Drawing.Color.Orchid,
			System.Drawing.Color.PaleGoldenrod,
			System.Drawing.Color.PaleGreen,
			System.Drawing.Color.PeachPuff,
			System.Drawing.Color.Peru,
			System.Drawing.Color.Pink,
			System.Drawing.Color.Plum,
			System.Drawing.Color.PowderBlue,
			System.Drawing.Color.Purple,
			System.Drawing.Color.Red,
			System.Drawing.Color.RosyBrown,
			System.Drawing.Color.RoyalBlue,
			System.Drawing.Color.SaddleBrown,
			System.Drawing.Color.Salmon,
			System.Drawing.Color.SeaGreen,
			System.Drawing.Color.Sienna,
			System.Drawing.Color.SkyBlue,
			System.Drawing.Color.SlateBlue,
			System.Drawing.Color.SlateGray,
			System.Drawing.Color.SpringGreen,
			System.Drawing.Color.Teal,
			System.Drawing.Color.Tomato,
			System.Drawing.Color.Turquoise,
			System.Drawing.Color.Violet,
			System.Drawing.Color.Wheat,
			System.Drawing.Color.Yellow,
			System.Drawing.Color.YellowGreen
		});

		public DelegateCommand<object> SeriesSelectAllCommand { get; private set; }

		public DelegateCommand<object> SeriesSelectNoneCommand { get; private set; }

		public DelegateCommand<object> SeriesSelectDefaultCommand { get; private set; }

		public ObservableCollection<IRenderableSeries> RenderableSeries { get; set; }

		public string ProcessInfo { get; set; }

		public int Count => RenderableSeries.Count;

		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public ProcessDataChartDataItem(int capacity = 300)
		{
			SeriesSelectAllCommand = new DelegateCommand<object>(OnSeriesSelectAll, null);
			SeriesSelectNoneCommand = new DelegateCommand<object>(OnSeriesSelectNone, null);
			SeriesSelectDefaultCommand = new DelegateCommand<object>(OnSeriesSelectDefault, null);
			RenderableSeries = new ObservableCollection<IRenderableSeries>();
			_capacity = capacity;
		}

		public void SetInfo(string info)
		{
			ProcessInfo = info;
			InvokePropertyChanged("ProcessInfo");
		}

		public void AppendData(Dictionary<string, object> data)
		{
			if (data == null)
			{
				return;
			}
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && data.ContainsKey(smartDataLine.DbDataName))
				{
					XyDataSeries<DateTime, float> val = smartDataLine.DataSeries as XyDataSeries<DateTime, float>;
					while (val.Count > _capacity)
					{
						val.RemoveAt(0);
					}
					val.Append(DateTime.Now, (float)Convert.ToDouble(data[smartDataLine.DbDataName]));
				}
			}
		}

		public void UpdateCultureString(Dictionary<string, string> data)
		{
			if (data == null)
			{
				return;
			}
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && data.ContainsKey(smartDataLine.DbDataName))
				{
					smartDataLine.DisplayName = data[smartDataLine.DbDataName];
				}
			}
		}

		public void UpdateData(List<HistoryDataItem> data)
		{
			ClearData();
			if (data == null || data.Count == 0)
			{
				return;
			}
			foreach (HistoryDataItem datum in data)
			{
				foreach (IRenderableSeries item in RenderableSeries)
				{
					if (item is SmartDataLine smartDataLine)
					{
						XyDataSeries<DateTime, float> val = smartDataLine.DataSeries as XyDataSeries<DateTime, float>;
						while (val.Count > _capacity)
						{
							val.RemoveAt(0);
						}
						if (!(smartDataLine.DbDataName != datum.dbName))
						{
							val.Append(datum.dateTime, (float)datum.value);
						}
					}
				}
			}
		}

		public void ClearData()
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine)
				{
					XyDataSeries<DateTime, float> val = smartDataLine.DataSeries as XyDataSeries<DateTime, float>;
					val.Clear();
				}
			}
		}

		public void UpdateColumns(Dictionary<string, Tuple<string, string, bool>> dicItems)
		{
			List<IRenderableSeries> list = new List<IRenderableSeries>();
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine)
				{
					if (!dicItems.ContainsKey(smartDataLine.DbDataName))
					{
						list.Add(item);
						continue;
					}
					UpdateColumn(dicItems[smartDataLine.DbDataName]);
					dicItems.Remove(smartDataLine.DbDataName);
				}
			}
			foreach (KeyValuePair<string, Tuple<string, string, bool>> dicItem in dicItems)
			{
				ObservableCollection<IRenderableSeries> renderableSeries = RenderableSeries;
				SmartDataLine smartDataLine2 = new SmartDataLine(dicItem.Value.Item2, dicItem.Value.Item3 ? Colors.Blue : Colors.Red, dicItem.Value.Item1, isVisable: true);
				smartDataLine2.YAxisId = dicItem.Value.Item3 ? "PressureYAxisId" : "GeneralYAxisId";
				renderableSeries.Add(smartDataLine2);
			}
			foreach (IRenderableSeries item2 in list)
			{
				RenderableSeries.Remove(item2);
			}
		}

		public void UpdateColumn(Tuple<string, string, bool> columnInfo)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && smartDataLine.DbDataName == columnInfo.Item1)
				{
					smartDataLine.DisplayName = columnInfo.Item2;
					if (smartDataLine.Stroke.Equals(System.Windows.Media.Color.FromArgb(byte.MaxValue, 0, 0, byte.MaxValue)))
					{
						System.Drawing.Color color = colorQueue.Peek();
						smartDataLine.Stroke = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
						colorQueue.Enqueue(colorQueue.Dequeue());
					}
					smartDataLine.YAxisId = columnInfo.Item3 ? "PressureYAxisId" : "GeneralYAxisId";
					break;
				}
			}
		}

		public void UpdateColumn(string dbName, string displayName, bool isPressure)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && smartDataLine.DbDataName == dbName)
				{
					smartDataLine.DisplayName = displayName;
					break;
				}
			}
		}

		public void UpdateColumnDisplayName(string dbName, string displayName)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && smartDataLine.DbDataName == dbName)
				{
					smartDataLine.DisplayName = displayName;
					break;
				}
			}
		}

		public void RemoveColumn(string dbName)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && smartDataLine.DbDataName == dbName)
				{
					RenderableSeries.Remove(smartDataLine);
					break;
				}
			}
		}

		public void RemoveAllColumn()
		{
			ClearData();
			RenderableSeries.Clear();
		}

		public void InitColumns(Dictionary<string, string> items)
		{
			ClearData();
			RenderableSeries.Clear();
			foreach (KeyValuePair<string, string> item in items)
			{
				ObservableCollection<IRenderableSeries> renderableSeries = RenderableSeries;
				SmartDataLine smartDataLine = new SmartDataLine(item.Key, Colors.Blue, item.Value, isVisable: true);
				smartDataLine.YAxisId = item.Key.Contains("Pressure") ? "PressureYAxisId" : "GeneralYAxisId";
				renderableSeries.Add(smartDataLine);
			}
		}

		public void SetColor(string key, System.Windows.Media.Color color)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine && smartDataLine.DisplayName == key)
				{
					smartDataLine.Stroke = color;
				}
			}
		}

		public void OnSeriesSelectAll(object param)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine)
				{
					smartDataLine.IsVisible = true;
				}
			}
		}

		public void OnSeriesSelectNone(object param)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine)
				{
					smartDataLine.IsVisible = false;
				}
			}
		}

		public void OnSeriesSelectDefault(object param)
		{
			foreach (IRenderableSeries item in RenderableSeries)
			{
				if (item is SmartDataLine smartDataLine)
				{
					smartDataLine.IsVisible = smartDataLine.IsDefaultVisable;
					smartDataLine.Stroke = smartDataLine.DefaultSeriesColor;
					smartDataLine.LineThickness = 1.0;
				}
			}
		}
	}
}
