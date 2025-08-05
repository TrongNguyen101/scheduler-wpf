using System.Globalization;
using System.Windows.Data;
namespace SchedulerWpfApp.Helper
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                if (intValue == 0)
                    return string.Empty;
                return intValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            if (int.TryParse(stringValue, out int result))
                return result;
            return 0;
        }
    }
}
