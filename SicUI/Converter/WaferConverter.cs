using Aitex.Core.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SicUI.Converter
{
	public class WaferConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			//if (values[0] != null && values[0] != DependencyProperty.UnsetValue)
			//{
			//	var status = (WaferStatus)values[0];
			//	var waferDisplayMode = (WaferDisplayMode)values[1];
			//	var waferOrigin = (string)values[2];
			//	var laserMarker = (string)values[3];
			//	var t7Code = (string)values[4];

			//	if (status != WaferStatus.Empty)
			//	{
			//		switch (waferDisplayMode)
			//		{
			//			case WaferDisplayMode.None:
			//				return string.Empty;
			//			case WaferDisplayMode.WaferOrigin:
			//				return waferOrigin;
			//			case WaferDisplayMode.LaserMarker:
			//				return laserMarker;
			//			case WaferDisplayMode.T7Code:
			//				return t7Code;
			//			default:
			//				break;
			//		}
			//		return string.Empty;
			//	}
			//}
			return string.Empty;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class WaferVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value == null || (WaferStatus)value == WaferStatus.Empty) ? Visibility.Hidden : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
