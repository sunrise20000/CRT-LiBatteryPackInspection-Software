using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SicUI.Models.RecipeEditors
{
    public class IsHighlightedToBorderThickness : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHighlighted)
                return isHighlighted ? new Thickness(3) : new Thickness(0);

            return value;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
