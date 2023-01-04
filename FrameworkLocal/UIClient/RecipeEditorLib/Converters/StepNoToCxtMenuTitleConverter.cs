using System;
using System.Globalization;
using System.Windows.Data;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.RecipeEditorLib
{
    internal class StepNoToCxtMenuTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is StepParam sp)
            {
                return $"Step No. {sp.Value}";
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
