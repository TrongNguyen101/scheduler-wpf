using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ViewModel
{
    public class TimetableCellViewModel : ViewBaseModel
    {
        #region Fields
        private Schedule? _schedule;
        private bool _isDragOver;
        private bool _isDragSource;
        private string _slotTime;
        #endregion

        #region Constructors
        public DateTime DayOfWeek { get; set; }

        public int SlotNumber { get; set; }
        public string SlotTime
        {
            get => _slotTime;
            set => SetProperty(ref _slotTime, value);
        }

        public Schedule? Schedule
        {
            get => _schedule;
            set => SetProperty(ref _schedule, value);
        }

        public bool IsDragOver
        {
            get => _isDragOver;
            set => SetProperty(ref _isDragOver, value);
        }

        public bool IsDragSource
        {
            get => _isDragSource;
            set => SetProperty(ref _isDragSource, value);
        }

        // Commands for drag and drop operations
        public ICommand MouseDownCommand { get; }
        public ICommand MouseMoveCommand { get; }
        public ICommand DragEnterCommand { get; }
        public ICommand DragLeaveCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand DragOverCommand { get; }

        // Reference to parent ViewModel for handling drag operations
        public CreateScheduleViewModel ParentViewModel { get; set; }

        public TimetableCellViewModel()
        {
            MouseDownCommand = new RelayCommandGeneric<MouseButtonEventArgs>(OnMouseDown);
            MouseMoveCommand = new RelayCommandGeneric<MouseEventArgs>(OnMouseMove);
            DragEnterCommand = new RelayCommandGeneric<DragEventArgs>(OnDragEnter);
            DragLeaveCommand = new RelayCommandGeneric<DragEventArgs>(OnDragLeave);
            DropCommand = new RelayCommandGeneric<DragEventArgs>(OnDrop);
            DragOverCommand = new RelayCommandGeneric<DragEventArgs>(OnDragOver);
        }

        private bool _isMousePressed = false;
        private Point _startPoint;
        #endregion

        #region Methods
        private void OnMouseDown(MouseButtonEventArgs e)
        {
            if (Schedule != null && e.LeftButton == MouseButtonState.Pressed)
            {
                _isMousePressed = true;
                _startPoint = e.GetPosition(null);
            }
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            if (_isMousePressed && Schedule != null)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Start drag operation
                    var dragData = new DataObject("Schedule", Schedule);
                    dragData.SetData("SourceCell", this);

                    IsDragSource = true; // Mark this cell as a drag source
                    DragDrop.DoDragDrop((DependencyObject)e.Source, dragData, DragDropEffects.Move);
                    IsDragSource = false; // Mark this cell as a no longer a drag source

                    _isMousePressed = false;
                }
            }
        }

        private void OnDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Schedule"))
            {
                var sourceCell = e.Data.GetData("SourceCell") as TimetableCellViewModel;
                if (sourceCell != this && CanAcceptDrop(e))
                {
                    IsDragOver = true;
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private void OnDragLeave(DragEventArgs e)
        {
            IsDragOver = false;
            e.Handled = true;
        }

        private void OnDragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Schedule"))
            {
                var sourceCell = e.Data.GetData("SourceCell") as TimetableCellViewModel;
                if (sourceCell != this && CanAcceptDrop(e))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private void OnDrop(DragEventArgs e)
        {
            IsDragOver = false;

            if (e.Data.GetDataPresent("Schedule"))
            {
                var droppedSchedule = e.Data.GetData("Schedule") as Schedule;
                var sourceCell = e.Data.GetData("SourceCell") as TimetableCellViewModel;

                if (sourceCell != null && droppedSchedule != null && CanAcceptDrop(e))
                {
                    // Perform the swap/move operation
                    ParentViewModel?.HandleScheduleDrop(sourceCell, this, droppedSchedule);
                }
            }
            e.Handled = true;
        }

        private bool CanAcceptDrop(DragEventArgs e)
        {
            var droppedSchedule = e.Data.GetData("Schedule") as Schedule;
            var sourceCell = e.Data.GetData("SourceCell") as TimetableCellViewModel;

            if (droppedSchedule == null || sourceCell == null)
                return false;

            // Check if it's the same cell
            if (sourceCell == this)
                return false;

            // Add your business logic here to validate if the drop is allowed
            // For example: same group, lecturer availability, etc.

            // Basic validation: same slot number (can only move within same time slot)
            //if (sourceCell.SlotNumber != this.SlotNumber)
            //    return false;

            // Additional validation can be added here
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Represents a row in the timetable, containing a slot number and a collection of timetable cells for that slot.
    /// </summary>
    public class SlotRowViewModel
    {
        public int SlotNumber { get; set; }
        public ObservableCollection<TimetableCellViewModel> Cells { get; set; } = new();
    }
}
