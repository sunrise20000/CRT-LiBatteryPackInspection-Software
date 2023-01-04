using System;
using System.Globalization;
using System.Windows.Data;

namespace Sicentury.Core.Converters
{
    public class ParameterNodeStatisticToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double dv)
            {
                if (double.IsNaN(dv) || double.IsInfinity(dv))
                    return "";

                return dv.ToString("F3");
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
