using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Converter that transforms a null/non-null value to a boolean value.
    /// Used in XAML bindings where you need to show/hide or enable/disable elements based on whether a value exists.
    /// </summary>
    public class NullToBooleanConverter : IValueConverter
    {
        #region Methods
        /// <summary>
        /// Converts a value to a boolean indicating whether the value is not null.
        /// </summary>
        /// <param name="value">The source data being passed to the target</param>
        /// <param name="targetType">The type of the target property</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture information (not used)</param>
        /// <returns>True if the value is not null, otherwise false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        /// <summary>
        /// Converts back from a boolean to the original value type.
        /// This operation is not supported in this converter.
        /// </summary>
        /// <exception cref="NotImplementedException">This method is not implemented</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
