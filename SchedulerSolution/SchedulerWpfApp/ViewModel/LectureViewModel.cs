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
    public class LectureViewModel : ViewBaseModel
    {
        // Dependencies injected via constructor
        private readonly InterfaceLecturerServices _lectureService;
        private readonly IExcelLectureImporter _excelImporter;
        private readonly IExcelLectureExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Lecturer> _lecture;
        private Lecturer? _selectedLecture;
        private string _searchKeyword;
        private bool _isLectureFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isLectureCodeEdit;
        private ObservableCollection<Lecturer> _allLectures;

        public string Title => SelectedLecture?.LecturerId != null ? "Chỉnh Sửa Giảng Viên" : "Thêm Mới Giảng Viên";
        public bool IsLectureCodeEdit
        {
            get => _isLectureCodeEdit;
            set => SetProperty(ref _isLectureCodeEdit, value);
        }

        // Observable collection to hold list of courses
        public ObservableCollection<Lecturer> Lectures
        {
            get => _lecture;
            set => SetProperty(ref _lecture, value);
        }

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
        public bool IsLectureFormOpen
        {
            get => _isLectureFormOpen;
            set => SetProperty(ref _isLectureFormOpen, value);
        }
        public bool IsOpenDialog
        {
            get => _isOpenDialog;
            set => SetProperty(ref _isOpenDialog, value);
        }
        public bool IsConfirmationOpen
        {
            get => _isConfirmationOpen;
            set => SetProperty(ref _isConfirmationOpen, value);
        }
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
        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportLectureCommand { get; }
        public ICommand ImportLectureCommand { get; }
        public ICommand AddLectureCommand { get; }
        public ICommand EditLectureCommand { get; }
        public ICommand DeleteLectureCommand { get; }
        public ICommand SaveLectureCommand { get; }
        public ICommand CancelEditLectureCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteLectureCommand { get; }

        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public LectureViewModel(InterfaceLecturerServices courseService, IExcelLectureExporter excelExporter, IExcelLectureImporter excelImporter)
        {
            _lectureService = courseService;
            _excelExporter = excelExporter;
            _excelImporter = excelImporter;

            Lectures = new ObservableCollection<Lecturer>();

            // Initialize commands with async methods
            LoadPeopleCommand = new RelayCommand(async () => await LoadLectureAsync());

            ImportLectureCommand = new RelayCommand(async () => await ImportLectureAsync());
            ExportLectureCommand = new RelayCommand(async () => await ExportLectureAsync());

            AddLectureCommand = new RelayCommand(async () => await AddLectureAsync());
            EditLectureCommand = new RelayCommandGeneric<Lecturer>(async (lecture) => await EditLectureAsync(lecture), (lecture) => lecture != null);

            // Generic command with parameter (used for deletion)
            DeleteLectureCommand = new RelayCommandGeneric<Lecturer>(async (lecture) => await DeleteLectureAsync(lecture), (lecture) => lecture != null);

            SaveLectureCommand = new RelayCommand(async () => await SaveLectureAsync());
            CancelEditLectureCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeleteLectureCommand = new RelayCommand(CancelDelete);

            // Load data immediately when ViewModel is constructed
            _ = LoadLectureAsync();
        }

        /// <summary>
        /// Loads lecture from the data service and populates the Lecture collection.
        /// </summary>
        private async Task LoadLectureAsync()
        {
            try
            {
                var lectureList = await _lectureService.GetAllLecturerAsync();
                _allLectures = new ObservableCollection<Lecturer>(lectureList);
                ResetToAllLectures();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load lectures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add a new course to the list
        private async Task AddLectureAsync()
        {
            SelectedLecture = new Lecturer(); // Khởi tạo object trống cho form
            IsLectureFormOpen = true;
            _isEdit = false;
            IsLectureCodeEdit = false;
        }

        // Edit a course existed
        private async Task EditLectureAsync(Lecturer lecture)
        {
            if (lecture == null) return;

            SelectedLecture = new Lecturer
            {
                LecturerId = lecture.LecturerId,
                LecturerName = lecture.LecturerName,
                Role = lecture.Role,
            };

            IsLectureFormOpen = true;
            _isEdit = true;
            IsLectureCodeEdit = true;
        }

        // Remove a course from the list
        private async Task DeleteLectureAsync(Lecturer lecture)
        {
            if (lecture == null) return;

            SelectedLecture = new Lecturer
            {
                LecturerId = lecture.LecturerId,
                LecturerName = lecture.LecturerName,
                Role = lecture.Role,
            };

            IsOpenDialog = true;
        }

        private async Task SaveLectureAsync()
        {
            if (SelectedLecture == null)
                return;

            if (string.IsNullOrWhiteSpace(SelectedLecture.LecturerId) || string.IsNullOrWhiteSpace(SelectedLecture.LecturerName) || string.IsNullOrWhiteSpace(SelectedLecture.Role))
            {
                MessageBox.Show("Mã Giảng viên không được để trống", "Cảnh báo");
                return;
            }

            try
            {
                // Check if _isEdit is false will create new course. Otherwise, update course
                if (!_isEdit)
                {
                    var existingLecture = Lectures.FirstOrDefault(s => s.LecturerId == SelectedLecture.LecturerId);

                    if (existingLecture == null)
                    {
                        await _lectureService.AddLecture(SelectedLecture);
                        MessageBox.Show("Thêm Giảng viên thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        // Cảnh báo
                        MessageBox.Show("Giảng viên đã tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var existingLecture = Lectures.FirstOrDefault(s => s.LecturerId == SelectedLecture.LecturerId);

                    if (existingLecture != null)
                    {
                        // Cập nhật thông tin
                        existingLecture.LecturerId = SelectedLecture.LecturerId;
                        existingLecture.LecturerName = SelectedLecture.LecturerName;
                        existingLecture.Role = SelectedLecture.Role;
                        await _lectureService.UpdateLecture(existingLecture);
                        MessageBox.Show("Cập nhật Giảng viên thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Cảnh báo
                        MessageBox.Show("Giảng viên không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu Giảng viên thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Đóng form và reset
                IsLectureFormOpen = false;
                SelectedLecture = null;
                LoadLectureAsync();
            }
        }
        public void CancelEdit()
        {
            IsLectureFormOpen = false;
            SelectedLecture = null;
        }
        public void CancelDelete()
        {
            IsOpenDialog = false;
        }
        // Delete lecture after confirmation
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
                await _lectureService.DeleteLecture(SelectedLecture.LecturerId);

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
            if (Lectures == null || Lectures.Count == 0)
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
                    var lectureList = Lectures.Where(p => p != null).ToList();
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
                    await _lectureService.ImportLectureFromExcel(data);
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
        /// Filter lecture with 3 column LectureCode, LectureName, Major
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
                // can search by ClassId, Category, Major
                var filtered = _allLectures.Where(lecture =>
                    (!string.IsNullOrEmpty(lecture.LecturerId) && lecture.LecturerId.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(lecture.LecturerName) && lecture.LecturerName.ToLower().Contains(lowerKeyword))
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
}
