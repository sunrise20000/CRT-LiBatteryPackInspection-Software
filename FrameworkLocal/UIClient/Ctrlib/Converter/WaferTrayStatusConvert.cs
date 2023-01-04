using System;
using System.Globalization;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.Ctrlib.Converter
{
	public class WaferTrayStatusConvert : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] != null && values[1] != null)
			{
				int waferStatus = (Int32)values[0];
				int trayStatus = (Int32)values[1];
				return (trayStatus != 0 && waferStatus==0) ? trayStatus+10 : waferStatus;
			}
			return false;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}