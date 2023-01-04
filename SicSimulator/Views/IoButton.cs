using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace SicSimulator.Views
{

    public class IoButton : ToggleButton
    {
        public static readonly DependencyProperty ONProperty;
        static IoButton()
        {
            ONProperty = DependencyProperty.Register("ON", typeof(bool), typeof(IoButton));
        }

        public bool ON
        {
            get { return (bool)GetValue(ONProperty); }
            set { SetValue(ONProperty, value); }
        }

    }



    public class BoolBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool? ret = (bool?)value;
            return ret.HasValue && ret.Value ? "LightBlue" : "Transparent";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;

        }
    }
}
