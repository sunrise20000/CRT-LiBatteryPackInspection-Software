using Aitex.Core.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SicUI.Converter
{
	public class WaferStatusConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			var status = (WaferStatus)value;
			switch (status)
			{
				case WaferStatus.Empty:
					return null;
				case WaferStatus.Normal:
					return Brushes.Green;
				case WaferStatus.Crossed:
					return Brushes.Orange;
				case WaferStatus.Double:
					return Brushes.OrangeRed;
				case WaferStatus.Unknown:
					return Brushes.Red;
				case WaferStatus.Dummy:
					return Brushes.Green;
				default:
					break;
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
