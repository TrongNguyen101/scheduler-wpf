using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using Microsoft.Win32;
using System.Windows;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel for managing lecture subjects, including adding, editing, deleting, importing, and exporting subjects.
    /// </summary>
    public class LectureSubjectViewModel : ViewBaseModel
    {
        #region Fields
        private readonly ILecturerSubjectServices _lectureSubjectService;
        private readonly ISubjectServices _subjectServices;
        private readonly ILecturerServices _lectureService;

        private ObservableCollection<LecturerSubject> _lecturesubject;
        private LecturerSubject? _selectedSubject;
        public string FormTitle => SelectedLecturerSubject?.Id == 0 ? "Thêm môn mới cho giảng viên" : "Chỉnh sửa môn cho giảng viên";
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
        public ObservableCollection<Lecturer> Lecturers { get; set; }
        public ObservableCollection<Subject> Subjects { get; set; }

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

        /// <summary>
        /// Gets or sets the currently selected lecturer subject.
        /// When set, updates related properties such as form title, selected lecturer ID, and selected subject ID.
        /// </summary>
        public LecturerSubject? SelectedLecturerSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetProperty(ref _selectedSubject, value))
                {
                    OnPropertyChanged(nameof(FormTitle)); // cập nhật tiêu đề form
                    OnPropertyChanged(nameof(SelectedLecturerId));
                    OnPropertyChanged(nameof(SelectedSubjectId));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected lecturer ID for the current lecturer subject.
        /// When set, updates the lecturer name of the selected lecturer subject.
        /// </summary>
        public string? SelectedLecturerId
        {
            get => SelectedLecturerSubject?.LecturerId;
            set
            {
                if (SelectedLecturerSubject != null && value != SelectedLecturerSubject.LecturerId)
                {
                    SelectedLecturerSubject.LecturerId = value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var lecturer = _lectureService.GetByLecturerCodeAsync(value).Result;
                        SelectedLecturerSubject.LecturerName = lecturer?.LecturerName ?? string.Empty;
                        OnPropertyChanged(nameof(SelectedLecturerSubject));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected subject code for the current lecturer subject.
        /// When set, updates the subject name of the selected lecturer subject.
        /// </summary>
        public string? SelectedSubjectId
        {
            get => SelectedLecturerSubject?.SubjectCode;
            set
            {
                if (SelectedLecturerSubject != null && value != SelectedLecturerSubject.SubjectCode)
                {
                    SelectedLecturerSubject.SubjectCode = value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var subjects = _subjectServices.GetBySubjectCodeAsync(value).Result;
                        SelectedLecturerSubject.SubjectName = subjects?.SubjectNameEnglish ?? string.Empty;
                        OnPropertyChanged(nameof(SelectedLecturerSubject));
                    }
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
        public LectureSubjectViewModel(ILecturerSubjectServices lectureSubjectService, ISubjectServices subjectServices, ILecturerServices lecturerServices)
        {
            _lectureSubjectService = lectureSubjectService;
            _subjectServices = subjectServices;
            _lectureService = lecturerServices;
            LectureSubjects = new ObservableCollection<LecturerSubject>();
            ImportLectureSubjectCommand = new RelayCommand(async () => await ImportLecturerSubjectListAsync());
            ExportLectureSubjectCommand = new RelayCommand(async () => await ExportLecturerSubjectAsync());
            EditLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await EditLecturerSubject(lectursubject));
            AddLectureSubjectCommand = new RelayCommand(async () => await AddLecturerSubjectAsync());
            DeleteLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await DeleteLecturerSubjectAsync(lectursubject));
            CancelEditLectureSubjectCommand = new RelayCommand(CancelEdit);
            CancelDeleteLectureSubjectCommand = new RelayCommand(CancelDelete);
            SaveLectureSubjectCommand = new RelayCommand(async () => await SaveLecturerSubjectAsync());
            ConfirmDeleteLectureSubjectCommand = new RelayCommand(async () => await ConfirmDeleteLecturerSubjectAsync());
            _ = LoadLecturerSubjects();
        }
        /// <summary>
        /// Asynchronously loads all lecture subjects from the service and populates the LectureSubjects collection.
        /// </summary>
        private async Task LoadLecturerSubjects()
        {
            try
            {
                var lecturesubjects = await _lectureSubjectService.GetAllAsync();
                var subjectList = await _subjectServices.GetAllAsync();
                var lecturerlist = await _lectureService.GetAllLecturerAsync();
                LectureSubjects = new ObservableCollection<LecturerSubject>(lecturesubjects);
                Subjects = new ObservableCollection<Subject>(subjectList);
                Lecturers = new ObservableCollection<Lecturer>(lecturerlist);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lấy dữ liệu không thành công: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Imports lecture subjects from an Excel file using the Excel importer service.
        /// </summary>
        private async Task ImportLecturerSubjectListAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // call ReadLectureSubjectFromExcel function to process file and read file when importing
                    var data = _lectureSubjectService.ReadLecturerSubjectFromExcel(dialog.FileName);
                    // call ImportGroupNameFromExcel function to add new data to database
                    await _lectureSubjectService.ImportLecturerSubjectFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadLecturerSubjects();
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
        private async Task ExportLecturerSubjectAsync()
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
                    var lectureSubjectList = await _lectureSubjectService.GetAllAsync();
                    // call ExportToLectureSubjectExcel function to export file
                    _lectureSubjectService.ExportToLecturerSubjectExcel(lectureSubjectList, dialog.FileName);
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
        public async Task EditLecturerSubject(LecturerSubject lecturerSubject)
        {
            SelectedLecturerSubject = new LecturerSubject
            {
                Id = lecturerSubject.Id,
                LecturerId = lecturerSubject.LecturerId,
                LecturerName = lecturerSubject.LecturerName,
                SubjectName = lecturerSubject.SubjectName,
                Major = lecturerSubject.Major,
                Term = lecturerSubject.Term,
                TotalSlots = lecturerSubject.TotalSlots,
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
        public async Task AddLecturerSubjectAsync()
        {
            SelectedLecturerSubject = new LecturerSubject();
            IsLectureSubjectFormOpen = true;
            // check if it is an edit event
            _isEditing = false;
        }

        /// <summary>
        /// Deletes the specified lecturer subject after confirmation.
        /// </summary>
        public async Task DeleteLecturerSubjectAsync(LecturerSubject lecturerSubject)
        {
            if (lecturerSubject == null) return;
            SelectedLecturerSubject = new LecturerSubject
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
            SelectedLecturerSubject = null;
            IsOpenDialog = false;
        }

        /// <summary>
        /// Cancels the edit operation and closes the lecture subject form.
        /// </summary>
        private void CancelEdit()
        {
            // set SelectedGroupname null 
            SelectedLecturerSubject = null;
            IsLectureSubjectFormOpen = false;
        }

        /// <summary>
        /// Saves the current lecturer subject, either adding a new one or updating an existing one.
        /// </summary>
        /// <returns></returns>
        public async Task SaveLecturerSubjectAsync()
        {
            if (SelectedLecturerSubject == null) return;
            if (string.IsNullOrWhiteSpace(SelectedLecturerSubject.LecturerId) ||
                string.IsNullOrWhiteSpace(SelectedLecturerSubject.SubjectCode) ||
                SelectedLecturerSubject.NumberOfClasses <= 0)
            {
                MessageBox.Show("Dữ liệu không được để trống.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                if (_isEditing)
                {
                    // Update existing lecturer subject
                    await _lectureSubjectService.UpdateAsync(SelectedLecturerSubject);
                    MessageBox.Show("Lecturer subject saved successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Add new lecturer subject
                    await _lectureSubjectService.AddAsync(SelectedLecturerSubject);
                    MessageBox.Show("Add Lecturer subject saved successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                IsLectureSubjectFormOpen = false;
                await LoadLecturerSubjects();
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
        public async Task ConfirmDeleteLecturerSubjectAsync()
        {
            if (SelectedLecturerSubject == null) return;
            try
            {
                await _lectureSubjectService.DeleteAsync(SelectedLecturerSubject.Id);
                MessageBox.Show("Lecturer subject deleted successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                IsOpenDialog = false;
                await LoadLecturerSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting lecturer subject: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion
}
