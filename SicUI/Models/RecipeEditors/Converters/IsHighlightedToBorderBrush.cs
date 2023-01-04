using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace SicUI.Models.RecipeEditors
{
    public class IsHighlightedToBorderBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHighlighted)
                return isHighlighted
                    ? new SolidColorBrush(Colors.Yellow)
                    : new SolidColorBrush(Colors.Transparent);

            return value;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
