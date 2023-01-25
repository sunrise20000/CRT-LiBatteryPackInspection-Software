﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Crt.UiCore.Converters
{
    internal class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
           if(value is bool boolValue)
               return boolValue;

           return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

