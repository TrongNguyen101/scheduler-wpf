using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// Base class for all view models in the application.
    /// Implements INotifyPropertyChanged to support binding in WPF.
    /// </summary>
    public class ViewBaseModel : INotifyPropertyChanged
    {
        #region Fields
        /// <summary>
        /// Event that is raised when a property value changes.
        /// WPF binding system subscribes to this event to be notified of property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Uses null-conditional operator (?.) to safely invoke the PropertyChanged event only if it has subscribers
            // Creates a new PropertyChangedEventArgs instance with the name of the property that changed
            // This notifies all bound UI elements that they should update their displayed values
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field of the property.</param>
        /// <param name="value">The new value for the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>True if the value was changed, false otherwise.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false; // Check if the new value is equal to the current value; if so, no update is needed
            field = value;                          // Update the backing field with the new value
            OnPropertyChanged(propertyName);        // Notify listeners that the property value has changed
            return true;                            // Indicate that the value was successfully updated
        }
        #endregion
    }
}
