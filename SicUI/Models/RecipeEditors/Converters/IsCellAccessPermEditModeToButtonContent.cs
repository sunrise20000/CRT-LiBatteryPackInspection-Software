using System;
using System.Globalization;
using System.Windows.Data;

namespace SicUI.Models.RecipeEditors
{
    internal class IsCellAccessPermEditModeToButtonContent : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCellAccessPermEditMode)
            {
                return isCellAccessPermEditMode ? "Exit" : "Cell-Perm";
            }

            return "Cell-Perm";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
