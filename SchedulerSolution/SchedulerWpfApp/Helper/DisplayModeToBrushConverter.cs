using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SchedulerWpfApp.Helper
{
    class DisplayModeToBrushConverter : IValueConverter
    {
        public Brush ActiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
        public Brush InactiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Light Gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return InactiveBrush;

            string currentMode = value.ToString();
            string targetMode = parameter.ToString();

            return currentMode == targetMode ? ActiveBrush : InactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
