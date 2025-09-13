using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Converter that transforms boolean values to Visibility enumeration values.
    /// Used in XAML bindings to show/hide elements based on boolean properties.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                var invert = parameter as string;
                if (!string.IsNullOrEmpty(invert) && invert.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                {
                    boolValue = !boolValue;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
