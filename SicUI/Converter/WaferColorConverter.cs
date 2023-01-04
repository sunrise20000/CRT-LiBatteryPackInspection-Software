using Aitex.Core.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SicUI.Converter
{
	public class WaferColorConverter : IMultiValueConverter
	{
		public Brush IdleWafer { get; set; }
		public Brush BusyWafer { get; set; }
		public Brush WaitWafer { get; set; }
		public Brush CompleteWafer { get; set; }
		public Brush ErrorWafer { get; set; }

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] == null || values[0] == DependencyProperty.UnsetValue)
				return null;

			var status = (WaferStatus)values[0];
			var state = (EnumWaferProcessStatus)values[1];
			var isDestination = (bool)values[2];
			var isSource = (bool)values[3];

			switch (status)
			{
				case WaferStatus.Empty:
					if (isDestination)
					{
						return BusyWafer;
					}
					return null;
				case WaferStatus.Normal:
					switch (state)
					{
						case EnumWaferProcessStatus.Idle:
							return IdleWafer;
						//case EnumWaferProcessStatus.Wait:
						//	return WaitWafer;
						case EnumWaferProcessStatus.InProcess:
							return BusyWafer;
						case EnumWaferProcessStatus.Completed:
							return CompleteWafer;
						case EnumWaferProcessStatus.Failed:
							return ErrorWafer;
						default:
							break;
					}
					return IdleWafer;
				case WaferStatus.Crossed:
					break;
				case WaferStatus.Double:
					break;
				case WaferStatus.Unknown:
					break;
				case WaferStatus.Dummy:
					if (isSource)
					{
						return WaitWafer;
					}
					return IdleWafer;
				default:
					break;
			}
			return IdleWafer;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
