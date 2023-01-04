using System;
using System.Windows;
using System.Windows.Data;
using MECF.Framework.Common.SubstrateTrackings;

namespace SicSimulator.Views.Converters
{
    public class CassetteVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameters, System.Globalization.CultureInfo culture)
        {
            CarrierInfo state = (CarrierInfo)value;
            return state.Status==CarrierStatus.Empty ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameters, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
