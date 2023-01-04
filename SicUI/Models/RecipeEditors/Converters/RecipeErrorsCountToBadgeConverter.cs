using System;
using System.Globalization;
using System.Windows.Data;

namespace SicUI.Models.RecipeEditors
{
    internal class RecipeErrorsCountToBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int cnt))
                return null;

            if (cnt == 0)
                return null;

            return cnt;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
