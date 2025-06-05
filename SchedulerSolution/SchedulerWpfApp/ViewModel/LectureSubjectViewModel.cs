using SchedulerWpfApp.Services;
using SchedulerWpfApp.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using Microsoft.Win32;
using System.Windows;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel for managing lecture subjects, including adding, editing, deleting, importing, and exporting subjects.
    /// </summary>
    public class LectureSubjectViewModel : ViewBaseModel
    {
        #region Fields
        private readonly ILectureSubjectService _lecturesubjectService;
        private readonly IExcelLectureSubjectImporter _excelImporter;
        private readonly IExcelLectureSubjectExporter _excelExporter;
        private ObservableCollection<LecturerSubject> _lecturesubject;
        private LecturerSubject? _selectedSubject;
        public string FormTitle => SelectedLectureSubject?.Id == 0 ? "Thêm môn mới cho giảng viên" : "Chỉnh sửa môn cho giảng viên";
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEditing;
        private bool _isLectureSubjectOpen;
        #endregion

        #region Construsctor
        /// <summary>
        /// Gets or sets the collection of lecture subjects displayed in the UI.
        /// </summary>
        public ObservableCollection<LecturerSubject> LectureSubjects
        {
            get => _lecturesubject;
            set => SetProperty(ref _lecturesubject, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the open dialog for adding/editing lecture subjects is currently open.
        /// </summary>
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the confirmation dialog for deleting a lecture subject is currently open.
        /// </summary>
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the lecture subject form for adding/editing is currently open.
        /// </summary>
        public bool IsLectureSubjectFormOpen
        {
            get => _isLectureSubjectOpen;
            set => SetProperty(ref _isLectureSubjectOpen, value);
        }

        public LecturerSubject SelectedLectureSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetProperty(ref _selectedSubject, value))
                {
                    OnPropertyChanged(nameof(FormTitle)); //Notify form title update
                }
            }
        }
        public ICommand ImportLectureSubjectCommand { get; }
        public ICommand ExportLectureSubjectCommand { get; }
        public ICommand EditLectureSubjectCommand { get; }
        public ICommand DeleteLectureSubjectCommand { get; }
        public ICommand AddLectureSubjectCommand { get; }
        public ICommand SaveLectureSubjectCommand { get; }
        public ICommand CancelEditLectureSubjectCommand { get; }
        public ICommand ConfirmDeleteLectureSubjectCommand { get; }
        public ICommand CancelDeleteLectureSubjectCommand { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the LectureSubjectViewModel class, setting up commands and loading initial data.
        /// </summary>
        public LectureSubjectViewModel(ILectureSubjectService lecturesubjectService, IExcelLectureSubjectImporter excelImporter, IExcelLectureSubjectExporter excelExporter)
        {
            _lecturesubjectService = lecturesubjectService;
            _excelImporter = excelImporter;
            _excelExporter = excelExporter;
            LectureSubjects = new ObservableCollection<LecturerSubject>();
            ImportLectureSubjectCommand = new RelayCommand(async () => await ImportLectureSubjectListAsync());
            ExportLectureSubjectCommand = new RelayCommand(async () => await ExportLectureSubjectAsync());
            EditLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await EditLectureSubject(lectursubject));
            AddLectureSubjectCommand = new RelayCommand(async () => await AddLectureSubjectAsync());
            DeleteLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await DeleteRoomAsync(lectursubject));
            CancelEditLectureSubjectCommand = new RelayCommand(CancelEdit);
            CancelDeleteLectureSubjectCommand = new RelayCommand(CancelDelete);
            SaveLectureSubjectCommand = new RelayCommand(async () => await SaveLectureSubjectAsync());
            ConfirmDeleteLectureSubjectCommand = new RelayCommand(async () => await ConfirmDeleteLectureSubjectAsync());
            _ = LoadLectureSubjects();
            _excelExporter = excelExporter;
        }
        /// <summary>
        /// Asynchronously loads all lecture subjects from the service and populates the LectureSubjects collection.
        /// </summary>
        private async Task LoadLectureSubjects()
        {
            try
            {
                var lecturesubjects = await _lecturesubjectService.GetAllAsync();
                LectureSubjects = new ObservableCollection<LecturerSubject>(lecturesubjects);
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show a message to the user)
                Console.WriteLine($"Error loading lecture subjects: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports lecture subjects from an Excel file using the Excel importer service.
        /// </summary>
        private async Task ImportLectureSubjectListAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // call ReadRoomFromExcel function to process file and read file when importing
                    var data = _excelImporter.ReadLectureSubjectFromExcel(dialog.FileName);
                    // call ImportGroupNameFromExcel function to add new data to database

                    await _lecturesubjectService.ImportLectureSubjectFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadLectureSubjects();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Exports the list of lecture subjects to an Excel file using the Excel exporter service.
        /// </summary>
        private async Task ExportLectureSubjectAsync()
        {
            if (LectureSubjects == null || LectureSubjects.Count == 0)
            {
                MessageBox.Show("No lecturesubject to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "LectureSubject.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    //Filter the GroupNames list to remove null elements
                    var roomList = LectureSubjects.Where(p => p != null).ToList();
                    // call ExportToExcelRoom function to export file
                    _excelExporter.ExportToExcel(roomList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Opens the form to edit a selected lecturer subject. 
        /// </summary>
        public async Task EditLectureSubject(LecturerSubject lecturerSubject)
        {
            SelectedLectureSubject = new LecturerSubject
            {
                Id = lecturerSubject.Id,
                LecturerId = lecturerSubject.LecturerId,
                LecturerName = lecturerSubject.LecturerName,
                SubjectCode = lecturerSubject.SubjectCode,
                NumberOfClasses = lecturerSubject.NumberOfClasses
            };
            IsLectureSubjectFormOpen = true;
            _isEditing = true;
        }

        /// <summary>
        /// Opens the form to add a new lecturer subject.
        /// </summary>
        /// <returns></returns>
        public async Task AddLectureSubjectAsync()
        {
            SelectedLectureSubject = new LecturerSubject();
            IsLectureSubjectFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
        }

        /// <summary>
        /// Deletes the specified lecturer subject after confirmation.
        /// </summary>
        public async Task DeleteRoomAsync(LecturerSubject lecturerSubject)
        {
            if (lecturerSubject == null) return;
            SelectedLectureSubject = new LecturerSubject
            {
                Id = lecturerSubject.Id,
                LecturerId = lecturerSubject.LecturerId,
                LecturerName = lecturerSubject.LecturerName,
                SubjectCode = lecturerSubject.SubjectCode,
                NumberOfClasses = lecturerSubject.NumberOfClasses
            };
            IsOpenDialog = true;
        }

        /// <summary>
        /// Cancels the delete operation and closes the confirmation dialog.
        /// </summary>
        private void CancelDelete()
        {
            // set SelectedGroupname null 
            SelectedLectureSubject = null;
            IsOpenDialog = false;
        }

        /// <summary>
        /// Cancels the edit operation and closes the lecture subject form.
        /// </summary>
        private void CancelEdit()
        {
            // set SelectedGroupname null 
            SelectedLectureSubject = null;
            IsLectureSubjectFormOpen = false;
        }

        /// <summary>
        /// Saves the current lecturer subject, either adding a new one or updating an existing one.
        /// </summary>
        /// <returns></returns>
        public async Task SaveLectureSubjectAsync()
        {
            if (SelectedLectureSubject == null) return;
            if (string.IsNullOrWhiteSpace(SelectedLectureSubject.LecturerId) ||
                string.IsNullOrWhiteSpace(SelectedLectureSubject.SubjectCode) ||
                SelectedLectureSubject.NumberOfClasses <= 0)
            {
                MessageBox.Show("Dữ liệu không được để trống.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                if (_isEditing)
                {
                    // Update existing lecturer subject
                    await _lecturesubjectService.UpdateAsync(SelectedLectureSubject);
                    MessageBox.Show("Lecturer subject saved successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Add new lecturer subject
                    await _lecturesubjectService.AddAsync(SelectedLectureSubject);
                    MessageBox.Show("Add Lecturer subject saved successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                IsLectureSubjectFormOpen = false;
                await LoadLectureSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving lecturer subject: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Confirms the deletion of the selected lecturer subject and deletes it from the database.
        /// </summary>
        /// <returns></returns>
        public async Task ConfirmDeleteLectureSubjectAsync()
        {
            if (SelectedLectureSubject == null) return;
            try
            {
                await _lecturesubjectService.DeleteAsync(SelectedLectureSubject.Id);
                MessageBox.Show("Lecturer subject deleted successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                IsOpenDialog = false;
                await LoadLectureSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting lecturer subject: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion
}
