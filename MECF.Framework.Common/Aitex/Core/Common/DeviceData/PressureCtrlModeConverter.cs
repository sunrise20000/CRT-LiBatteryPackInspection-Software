using System;
using System.Globalization;
using System.Windows.Data;

namespace Aitex.Core.Common.DeviceData
{
	public class PressureCtrlModeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}
			return (PressureCtrlMode)value switch
			{
				PressureCtrlMode.TVOpen => "Open", 
				PressureCtrlMode.TVClose => "Close", 
				PressureCtrlMode.TVPositionCtrl => "Position", 
				PressureCtrlMode.TVPressureCtrl => "Pressure", 
				PressureCtrlMode.TVCalib => "Calibration", 
				PressureCtrlMode.Undefined => "Unknown", 
				_ => null, 
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
