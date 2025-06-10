using System.Collections.ObjectModel;
using SchedulerWpfApp.Helper;
using System.Windows.Input;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using Microsoft.Win32;
using System.Windows;
using SchedulerWpfApp.Services.LecturerSubjectServices;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing lecturers, including adding, editing, deleting, and exporting lecturers.
    /// </summary>
    public class LectureViewModel : ViewBaseModel
    {
        #region Fields
        // Dependencies injected via constructor
        private readonly InterfaceLecturerServices _lecturerService;
        private readonly IExcelLectureImporter _excelImporter;
        private readonly IExcelLectureExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Lecturer> _lecturers;
        private Lecturer? _selectedLecture;
        private string _searchKeyword;
        private bool _isLectureFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isLectureCodeEdit;
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
        public LectureViewModel(InterfaceLecturerServices courseService, IExcelLectureExporter excelExporter, IExcelLectureImporter excelImporter)
        {
            _lecturerService = courseService;
            _excelExporter = excelExporter;
            _excelImporter = excelImporter;

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
                MessageBox.Show($"Failed to load lectures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Adds a new lecturer by initializing the SelectedLecture property and opening the form for input.
        /// </summary>
        private async Task AddLectureAsync()
        {
            SelectedLecture = new Lecturer(); // Initialize a new Lecturer object
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

            if (string.IsNullOrWhiteSpace(SelectedLecture.LecturerId) || string.IsNullOrWhiteSpace(SelectedLecture.LecturerName) || string.IsNullOrWhiteSpace(SelectedLecture.Role) || string.IsNullOrWhiteSpace(SelectedLecture.Department))
            {
                MessageBox.Show("Thông tin giảng viên không được để trống", "Cảnh báo");
                IsLectureFormOpen = true;
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
                        await _lecturerService.AddLecture(SelectedLecture);
                        MessageBox.Show("Thêm giảng viên thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        // Warning if lecturer already exists
                        MessageBox.Show("Giảng viên đã tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                        // Update other properties as needed
                        await _lecturerService.UpdateLecture(existingLecture);
                        MessageBox.Show("Cập nhật giảng viên thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Warning if lecturer does not exist
                        MessageBox.Show("Giảng viên không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu giảng viên thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Close form and reset
                IsLectureFormOpen = false;
                SelectedLecture = null;
                LoadLectureAsync();
            }
        }

        /// <summary>
        /// Cancels the edit operation, closes the lecturer form, and resets the selected lecturer.
        /// </summary>
        public void CancelEdit()
        {
            IsLectureFormOpen = false;
            SelectedLecture = null;
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
                MessageBox.Show("Không có Giảng viên nào được chọn để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsOpenDialog = false;
                return;
            }

            try
            {
                // Call the service to delete the lecturer
                await _lecturerService.DeleteLecture(SelectedLecture.LecturerId);

                MessageBox.Show("Xóa Giảng viên thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xóa Giảng viên thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("No Lectures to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    _excelExporter.ExportToExcel(lectureList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _excelImporter.ReadLecturesFromExcel(dialog.FileName);
                    await _lecturerService.ImportLectureFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadLectureAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    (!string.IsNullOrEmpty(lecturer.LecturerName) && lecturer.LecturerName.ToLower().Contains(lowerKeyword))
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
