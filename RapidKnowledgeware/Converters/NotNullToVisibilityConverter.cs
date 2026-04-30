using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RapidKnowledgeware.Converters
{
    /// <summary>
    /// 非空即隐藏转换器
    /// </summary>
    public class NotNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}