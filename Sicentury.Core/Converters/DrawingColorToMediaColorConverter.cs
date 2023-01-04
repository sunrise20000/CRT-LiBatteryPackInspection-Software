using System;
using System.Globalization;
using System.Windows.Data;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace Sicentury.Core.Converters
{
    internal class DrawingColorToMediaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DrawingColor dc)
                return MediaColor.FromArgb(dc.A, dc.R, dc.G, dc.B);

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MediaColor mc)
                return DrawingColor.FromArgb(mc.A, mc.R, mc.G, mc.B);

            return value;
        }
    }
}
