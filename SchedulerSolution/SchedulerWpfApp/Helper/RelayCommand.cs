using System.Windows.Input;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Implementation of <see cref="ICommand"/> that wraps a method delegate to enable commanding in MVVM pattern.
    /// Provides a simple way to bind commands to methods in view models.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields
        /// <summary>
        /// The action to execute when the command is invoked.
        /// </summary>
        private readonly Action _execute;

        /// <summary>
        /// Optional predicate that determines if the command can be executed.
        /// </summary>
        private readonly Func<bool>? _canExecute;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional predicate that determines if the command can be executed.</param>
        /// <exception cref="ArgumentNullException">Thrown if the execute action is null.</exception>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Event raised when the return value of the <see cref="CanExecute"/> method changes.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can be executed in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        /// <returns>True if the command can be executed; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
        public void Execute(object? parameter)
        {
            _execute();
        }

        /// <summary>
        /// Forces the command to raise the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion
    }
}
