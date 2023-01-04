using System;
using System.Globalization;
using System.Windows.Data;
using RecipeEditorLib.RecipeModel.Params;

namespace SicUI.Models.RecipeEditors
{
    public class ParamToCellTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IParam param)) 
                return null;

            if (!param.IsValidated) 
                return param.ValidationError;

            if (param is DoubleParam dblParam)
                return $"{dblParam.Minimun} - {dblParam.Maximun}";

            return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
