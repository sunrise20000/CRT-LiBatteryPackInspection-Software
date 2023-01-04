using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SicUI.Models.RecipeEditors
{
    internal class IsSavedToNameMarkVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChanged)
            {
                if (parameter?.ToString() == "R")
                    isChanged = !isChanged;

                return isChanged ? Visibility.Visible : Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
