using System;
using System.Globalization;
using System.Windows.Data;
using ExtendedGrid.Microsoft.Windows.Controls;

namespace SicUI.Models.RecipeEditors
{
    internal class IsCellAccessPermEditModeToDgSelectionUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCellAccessPermEditMode)
            {
                return isCellAccessPermEditMode ? DataGridSelectionUnit.Cell : DataGridSelectionUnit.FullRow;
            }

            return DataGridSelectionUnit.FullRow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
