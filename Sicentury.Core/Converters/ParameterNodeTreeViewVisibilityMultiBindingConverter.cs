using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sicentury.Core.Converters
{
    internal class ParameterNodeTreeViewVisibilityMultiBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果绑定的参数个数错误，隐藏节点
            if (values.Length != 2)
            {
                Debugger.Break();
                return Visibility.Collapsed;
            }

            if (values[0] is bool isMatch && values[1] is Visibility visibility)
            {
                // 如果设置为隐藏，则无论是否Filter匹配，均隐藏节点
                if (visibility != Visibility.Visible)
                    return visibility;

                // 如果设置为显示，则根据Filter是否匹配决定是否显示节点
                if (isMatch == false)
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }

            // 如果传入的参数类型错误，隐藏节点
            Debugger.Break();
            return Visibility.Collapsed;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
