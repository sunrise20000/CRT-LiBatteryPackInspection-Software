using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using RecipeEditorLib.RecipeModel.Params;

namespace SicUI.Models.RecipeEditors
{
    public class SelectedRecipeStepsToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<RecipeStep> list)
                return (IList)list;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList list)
            {
                return list.Cast<RecipeStep>().ToList();
            }

            return null;
        }
    }
}
