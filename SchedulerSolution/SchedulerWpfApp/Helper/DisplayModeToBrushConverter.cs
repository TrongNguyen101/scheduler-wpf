using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using static SchedulerWpfApp.ViewModel.CreateScheduleViewModel;

namespace SchedulerWpfApp.Helper
{
    class DisplayModeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DisplayMode current && parameter is string target)
            {
                return current.ToString() == target ? Brushes.YellowGreen : Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
