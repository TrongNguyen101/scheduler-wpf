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
        #region Methods
        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert</param>
        /// <param name="targetType">The type of the binding target property</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>Visibility.Visible if value is true, otherwise Visibility.Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility value back to a boolean value.
        /// Not implemented in this converter.
        /// </summary>
        /// <exception cref="NotImplementedException">This method is not implemented</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
