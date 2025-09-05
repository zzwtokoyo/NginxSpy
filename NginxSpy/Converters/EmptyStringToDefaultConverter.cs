using System;
using System.Globalization;
using System.Windows.Data;

namespace NginxSpy.Converters
{
    public class EmptyStringToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrEmpty(str) ? (parameter ?? "无") : str;
            }
            
            return parameter ?? "无";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}