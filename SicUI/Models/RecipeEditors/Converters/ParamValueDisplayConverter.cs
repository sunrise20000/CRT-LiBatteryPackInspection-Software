using System;
using System.Globalization;
using System.Windows.Data;

namespace SicUI.Models.RecipeEditors
{
    internal class ParamValueDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            // value[0] must be Param.Value
            // value[1] must be Param.HasPermission
            if (value == null || value.Length != 2) 
                return value;
            
            if (value[1] is bool isHideValue && value[0] != null)
            {
                return isHideValue ? "***" : value[0].ToString();
            }
                
            return value[0];

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
