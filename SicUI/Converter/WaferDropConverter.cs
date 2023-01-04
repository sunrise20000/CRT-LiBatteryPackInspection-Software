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
	public class WaferDropConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] != null && values[0] != DependencyProperty.UnsetValue)
			{
				var status = (WaferStatus)values[0];
				var isDestination = (bool)values[1];
				return (status == WaferStatus.Empty || status == WaferStatus.Dummy) && !isDestination;
			}
			return false;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
