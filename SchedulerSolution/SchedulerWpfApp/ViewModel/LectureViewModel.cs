using System.Collections.ObjectModel;
using SchedulerWpfApp.Helper;
using System.Windows.Input;
using SchedulerWpfApp.Model;
using Microsoft.Win32;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing lecturers, including adding, editing, deleting, and exporting lecturers.
    /// </summary>
    public class LectureViewModel : ViewBaseModel
    {
        #region Fields
        // Dependencies injected via constructor
        private readonly ILecturerServices _lecturerService;
        private readonly INotificationService _notificationService;
        private readonly ILecturerSubjectServices _lecturerSubjectService;
        // Internal data fields
        private ObservableCollection<Lecturer> _lecturers;
        private Lecturer? _selectedLecture;
        private string _searchKeyword;
        private bool _isLectureFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isLectureCodeEdit;
        private int _progressValue;
        private bool _isProgressBarOpen;
        private ObservableCollection<Lecturer> _allLectures;
        public string Title => SelectedLecture?.LecturerId != null ? "Chỉnh Sửa Giảng Viên" : "Thêm Mới Giảng Viên";
        #endregion

        #region Constructor
        /// <summary>
        /// ViewModel for managing lecturers, including adding, editing, deleting, and exporting lecturers.
        /// </summary>
        public bool IsLectureCodeEdit
        {
            get => _isLectureCodeEdit;
            set => SetProperty(ref _isLectureCodeEdit, value);
        }

        /// <summary>
        /// Collection of lecturers to be displayed in the UI.
        /// </summary>
        public ObservableCollection<Lecturer> Lectures
        {
            get => _lecturers;
            set => SetProperty(ref _lecturers, value);
        }

        /// <summary>
        /// Selected lecturer for editing or viewing details.
        /// </summary>
        public Lecturer SelectedLecture
        {
            get => _selectedLecture;
            set
            {
                if (SetProperty(ref _selectedLecture, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Indicates whether the form for adding or editing a lecturer is open.
        /// </summary>
        public bool IsLectureFormOpen
        {
            get => _isLectureFormOpen;
            set => SetProperty(ref _isLectureFormOpen, value);
        }

        /// <summary>
        /// Indicates whether the dialog for adding or editing a lecturer is open.
        /// </summary>
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }

        /// <summary>
        /// Indicates whether the confirmation dialog for deletion is open.
        /// </summary>
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }

        /// <summary>
        /// Search keyword for filtering lectures.
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function fillter list by keyword
                    FilterLectures();
                }
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public bool IsProgressBarOpen
        {
            get => _isProgressBarOpen;
            set => SetProperty(ref _isProgressBarOpen, value);
        }

        // Commands exposed to the View
        public ICommand LoadLecturerCommand { get; }
        public ICommand ExportLectureCommand { get; }
        public ICommand ImportLectureCommand { get; }
        public ICommand AddLectureCommand { get; }
        public ICommand EditLectureCommand { get; }
        public ICommand DeleteLectureCommand { get; }
        public ICommand SaveLectureCommand { get; }
        public ICommand CancelEditLectureCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteLectureCommand { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public LectureViewModel(ILecturerServices lecturerService, INotificationService notificationService, ILecturerSubjectServices lecturerSubjectServices)
        {
            _lecturerService = lecturerService;
            _notificationService = notificationService;
            _lecturerSubjectService = lecturerSubjectServices;

            Lectures = new ObservableCollection<Lecturer>();

            // Initialize commands with async methods
            LoadLecturerCommand = new RelayCommand(async () => await LoadLectureAsync());

            ImportLectureCommand = new RelayCommand(async () => await ImportLectureAsync());
            ExportLectureCommand = new RelayCommand(async () => await ExportLectureAsync());

            AddLectureCommand = new RelayCommand(async () => await AddLectureAsync());
            EditLectureCommand = new RelayCommandGeneric<Lecturer>(async (lecturer) => await EditLectureAsync(lecturer), (lecturer) => lecturer != null);

            // Generic command with parameter (used for deletion)
            DeleteLectureCommand = new RelayCommandGeneric<Lecturer>(async (lecturer) => await DeleteLectureAsync(lecturer), (lecturer) => lecturer != null);

            SaveLectureCommand = new RelayCommand(async () => await SaveLectureAsync());
            CancelEditLectureCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeleteLectureCommand = new RelayCommand(CancelDelete);

            SelectedLecture = new Lecturer(); // Initialize a new Lecturer object
            // Load data immediately when ViewModel is constructed
            _ = LoadLectureAsync();
        }

        /// <summary>
        /// Loads lecturer from the data service and populates the Lecture collection.
        /// </summary>
        private async Task LoadLectureAsync()
        {
            try
            {
                var lectureList = await _lecturerService.GetAllLecturerAsync();
                _allLectures = new ObservableCollection<Lecturer>(lectureList);
                ResetToAllLectures();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Có lỗi khi tải danh sách giảng viên.");
            }
        }

        /// <summary>
        /// Adds a new lecturer by initializing the SelectedLecture property and opening the form for input.
        /// </summary>
        private async Task AddLectureAsync()
        {
            IsLectureFormOpen = true;
            _isEdit = false;
            IsLectureCodeEdit = false;
        }

        /// <summary>
        /// Edits the selected lecturer by setting it to the SelectedLecture property and opening the form for editing.
        /// </summary>
        /// <param name="lecturer"></param>
        private async Task EditLectureAsync(Lecturer lecturer)
        {
            if (lecturer == null) return;

            SelectedLecture = new Lecturer
            {
                LecturerId = lecturer.LecturerId,
                LecturerName = lecturer.LecturerName,
                Role = lecturer.Role,
                Department = lecturer.Department,
                LecturerAccount = lecturer.LecturerAccount
            };

            IsLectureFormOpen = true;
            _isEdit = true;
            IsLectureCodeEdit = true;
        }

        /// <summary>
        /// Deletes the specified lecturer after confirmation.
        /// </summary>
        /// <param name="lecturer"></param>
        private async Task DeleteLectureAsync(Lecturer lecturer)
        {
            if (lecturer == null) return;

            SelectedLecture = new Lecturer
            {
                LecturerId = lecturer.LecturerId,
                LecturerName = lecturer.LecturerName,
                Role = lecturer.Role,
                Department = lecturer.Department,
                LecturerAccount = lecturer.LecturerAccount
            };

            IsOpenDialog = true;
        }

        /// <summary>
        /// Saves the selected lecturer to the data source, either adding a new one or updating an existing one.
        /// </summary>
        private async Task SaveLectureAsync()
        {
            if (SelectedLecture == null)
                return;

            if (string.IsNullOrWhiteSpace(SelectedLecture.LecturerId) || string.IsNullOrWhiteSpace(SelectedLecture.LecturerName) || string.IsNullOrWhiteSpace(SelectedLecture.Role) || string.IsNullOrWhiteSpace(SelectedLecture.Department) || string.IsNullOrWhiteSpace(SelectedLecture.LecturerAccount))
            {
                _notificationService.ShowWarning("Thông tin giảng viên không được để trống");
                return;
            }

            try
            {
                // Check if _isEdit is false will create new course. Otherwise, update course
                if (!_isEdit)
                {
                    // Check if the lecturer already exists in the collection
                    var existingLecture = _allLectures.FirstOrDefault(s => s.LecturerId == SelectedLecture.LecturerId);

                    if (existingLecture == null)
                    {
                        // Add new lecturer
                        await _lecturerService.AddLecturer(SelectedLecture);
                        _notificationService.ShowSuccess("Thêm giảng viên thành công");
                    }
                    else
                        // Warning if lecturer already exists
                        _notificationService.ShowWarning("Giảng viên đã tồn tại trong hệ thống. Vui lòng kiểm tra lại mã giảng viên.");
                }
                else
                {
                    // Check if the lecturer exists in the collection
                    var existingLecture = _allLectures.FirstOrDefault(s => s.LecturerId == SelectedLecture.LecturerId);

                    if (existingLecture != null)
                    {
                        // Update existing lecturer
                        existingLecture.LecturerId = SelectedLecture.LecturerId;
                        existingLecture.LecturerName = SelectedLecture.LecturerName;
                        existingLecture.Role = SelectedLecture.Role;
                        existingLecture.Department = SelectedLecture.Department;
                        existingLecture.LecturerAccount = SelectedLecture.LecturerAccount;

                        // Update other properties as needed
                        await _lecturerService.UpdateLecturer(existingLecture);
                        _notificationService.ShowSuccess("Cập nhật giảng viên thành công");
                    }
                    else
                    {
                        // Warning if lecturer does not exist
                        _notificationService.ShowWarning("Giảng viên không tồn tại trong hệ thống. Vui lòng kiểm tra lại mã giảng viên.");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lưu giảng viên thất bại.");
            }
            finally
            {
                // Close form and reset
                IsLectureFormOpen = false;
                SelectedLecture = new Lecturer();
                LoadLectureAsync();
            }
        }

        /// <summary>
        /// Cancels the edit operation, closes the lecturer form, and resets the selected lecturer.
        /// </summary>
        public void CancelEdit()
        {
            IsLectureFormOpen = false;
            SelectedLecture = new Lecturer();
        }

        /// <summary>
        /// Cancels the deletion of the selected lecturer and closes the confirmation dialog.
        /// </summary>
        public void CancelDelete()
        {
            IsOpenDialog = false;
        }

        /// <summary>
        /// Confirms the deletion of the selected lecturer and removes it from the data source.
        /// </summary>
        private async Task ConfirmDeleteAsync()
        {
            if (SelectedLecture == null)
            {
                _notificationService.ShowWarning("Không có Giảng viên nào được chọn để xóa.");
                IsOpenDialog = false;
                return;
            }

            try
            {
                bool lecturerExistsInLecturerSubject = await _lecturerSubjectService.CheckLecturerExits(SelectedLecture.LecturerId);
                if (lecturerExistsInLecturerSubject)
                {
                    _notificationService.ShowWarning("Giảng Viên này đang có lịch phân công. Không thể xóa.");
                    return;
                }
                // Call the service to delete the lecturer
                await _lecturerService.DeleteLecturer(SelectedLecture.LecturerId);

                _notificationService.ShowSuccess("Xóa Giảng viên thành công");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Xóa Giảng viên thất bại.");
            }
            finally
            {
                IsOpenDialog = false;
                SelectedLecture = null;
                LoadLectureAsync();
            }
        }

        /// <summary>
        /// Exports the current list of lectures to an Excel file.
        /// </summary>
        private async Task ExportLectureAsync()
        {
            if (_allLectures == null || _allLectures.Count == 0)
            {
                _notificationService.ShowWarning("Không có giảng viên nào để xuất.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Lectures.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var lectureList = _allLectures.Where(p => p != null).ToList();
                    _lecturerService.ExportToExcel(lectureList, dialog.FileName);
                    _notificationService.ShowSuccess("Xuất giảng viên thành công!");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Xuất giảng viên thất bại.");
                }
            }
        }

        /// <summary>
        /// Imports lectures from an Excel file and adds them to the data source.
        /// </summary>
        private async Task ImportLectureAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            var progress = new Progress<int>(percentCompleted =>
            {
                ProgressValue = percentCompleted;
            });

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _lecturerService.ReadLecturersFromExcel(dialog.FileName);

                    IsProgressBarOpen = true;
                    await _lecturerService.ImportLecturerFromExcel(data, progress);
                    IsProgressBarOpen = false;

                    _notificationService.ShowSuccess("Nhập giảng viên thành công!");
                    await LoadLectureAsync();
                }
                catch (Exception ex)
                {
                    IsProgressBarOpen = false;
                    _notificationService.ShowError($"Nhập giảng viên thất bại. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Filter lecturer with 3 column LectureCode, LectureName, Major
        /// </summary>
        private void FilterLectures()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                ResetToAllLectures();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by LecturerId, LecturerName
                var filtered = _allLectures.Where(lecturer =>
                    (!string.IsNullOrEmpty(lecturer.LecturerId) && lecturer.LecturerId.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(lecturer.LecturerName) && lecturer.LecturerName.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(lecturer.LecturerAccount) && lecturer.LecturerAccount.ToLower().Contains(lowerKeyword))
                ).ToList();

                Lectures = new ObservableCollection<Lecturer>(filtered);
            }
        }

        /// <summary>
        /// Reset Lectures
        /// </summary>
        private void ResetToAllLectures()
        {
            Lectures = new ObservableCollection<Lecturer>(_allLectures);
        }
    }
    #endregion
}
