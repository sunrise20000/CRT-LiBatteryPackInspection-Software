using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace MECF.Framework.UI.Client.RecipeEditorLib
{
    internal class IsDgSelectionCountEqualsOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList selection)
            {
                return selection.Count == 1;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
