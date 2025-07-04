using System.Globalization;
using static SchedulerWpfApp.ViewModel.CreateScheduleViewModel;
using System.Windows;
using System.Windows.Data;

namespace SchedulerWpfApp.Helper
{
    class DisplayModeToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DisplayMode current && parameter is string target)
            {
                return current.ToString() == target ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
