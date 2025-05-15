using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    public class CourseViewModel : ViewBaseModel
    {
        // Dependencies injected via constructor
        private readonly ICourseService _courseService;
        private readonly IExcelSubjectImporter _excelImporter;
        private readonly IExcelSubjectExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Subject> _subject;
        private Subject? _selectedSubject;
        private string _searchKeyword;
        private bool _isSubjectFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;

        // Observable collection to hold list of courses
        public ObservableCollection<Subject> Subjects
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set => SetProperty(ref _selectedSubject, value);
        }
        public bool IsSubjectFormOpen
        {
            get => _isSubjectFormOpen;
            set => SetProperty(ref _isSubjectFormOpen, value);
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

        public ICommand LoadPeopleCommand { get; }
        public ICommand ExportSubjectCommand { get; }
        public ICommand ImportSubjectCommand { get; }
        public ICommand AddSubjectCommand { get; }
        public ICommand EditSubjectCommand { get; }
        public ICommand DeleteSubjectCommand { get; }
        public ICommand SaveSubjectCommand { get; }
        public ICommand CancelEditSubjectCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteSubjectCommand { get; }

        public CourseViewModel(ICourseService courseService, IExcelSubjectExporter excelExporter, IExcelSubjectImporter excelImporter)
        {
            _courseService = courseService;
            _excelExporter = excelExporter;
            _excelImporter = excelImporter;

            Subjects = new ObservableCollection<Subject>();

            // Initialize commands with async methods
            LoadPeopleCommand = new RelayCommand(async () => await LoadSubjectAsync());

            ImportSubjectCommand = new RelayCommand(async () => await ImportSubjectAsync());
            ExportSubjectCommand = new RelayCommand(async () => await ExportSubjectAsync());

            AddSubjectCommand = new RelayCommand(async () => await AddSubjectAsync());
            EditSubjectCommand = new RelayCommandGeneric<Subject>(async (subject) => await EditSubjectAsync(subject), (subject) => subject != null);

            // Generic command with parameter (used for deletion)
            DeleteSubjectCommand = new RelayCommandGeneric<Subject>(async (subject) => await DeleteSubjectAsync(subject), (subject) => subject != null);

            SaveSubjectCommand = new RelayCommand(async () => await SaveSubjectAsync());
            CancelEditSubjectCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeleteSubjectCommand = new RelayCommand(CancelDelete);

            // Load data immediately when ViewModel is constructed
            _ = LoadSubjectAsync();

        }
        private async Task LoadSubjectAsync()
        {
            try
            {
                var subjectList = await _courseService.GetAllAsync();
                Subjects = new ObservableCollection<Subject>(subjectList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load subjects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add a new course to the list
        private async Task AddSubjectAsync()
        {
            SelectedSubject = new Subject(); // Khởi tạo object trống cho form
            IsSubjectFormOpen = true;
        }

        private async Task EditSubjectAsync(Subject subject)
        {
            if (subject == null) return;

            SelectedSubject = new Subject
            {
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Major = subject.Major,
                TotalSessions = subject.TotalSessions,
                SlotsPerWeek = subject.SlotsPerWeek,
                SemesterId = subject.SemesterId,
            };

            IsSubjectFormOpen = true;
        }

        // Remove a course from the list
        private async Task DeleteSubjectAsync(Subject subject)

        {
            if (subject == null) return;

            SelectedSubject = new Subject
            {
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Major = subject.Major,
                TotalSessions = subject.TotalSessions,
                SlotsPerWeek = subject.SlotsPerWeek,
                SemesterId = subject.SemesterId,
            };

            IsOpenDialog = true;

        }

        private async Task SaveSubjectAsync()
        {
            if (SelectedSubject == null)
                return;

            try
            {
                var existingSubject = Subjects.FirstOrDefault(s => s.SubjectCode == SelectedSubject.SubjectCode);

                if (existingSubject != null)
                {
                    // Cập nhật thông tin
                    existingSubject.SubjectName = SelectedSubject.SubjectName;
                    existingSubject.Major = SelectedSubject.Major;
                    existingSubject.TotalSessions = SelectedSubject.TotalSessions;
                    existingSubject.SlotsPerWeek = SelectedSubject.SlotsPerWeek;
                    existingSubject.SemesterId = SelectedSubject.SemesterId;

                    await _courseService.UpdateSubject(existingSubject);
                }
                else
                {
                    // Cảnh báo
                    MessageBox.Show("Môn học không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu môn học thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Đóng form và reset
                IsSubjectFormOpen = false;
                SelectedSubject = null;
                LoadSubjectAsync();
            }
        }
        public void CancelEdit()
        {
            IsSubjectFormOpen = false;
            SelectedSubject = null;

        }
        public void CancelDelete()
        {

            IsOpenDialog = false;
        }
        private async Task ConfirmDeleteAsync()
        {
            if (SelectedSubject == null)
            {
                MessageBox.Show("Không có môn học nào được chọn để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsOpenDialog = false;
                return;
            }

            try
            {
                await _courseService.DeleteSubject(SelectedSubject.SubjectCode);

                MessageBox.Show("Xóa môn học thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xóa môn học thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsOpenDialog = false;
                SelectedSubject = null;
                LoadSubjectAsync();
            }

        }
        private async Task ExportSubjectAsync()
        {
            if (Subjects == null || Subjects.Count == 0)
            {
                MessageBox.Show("No subject to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Subjects.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var subjectList = Subjects.Where(p => p != null).ToList();
                    _excelExporter.ExportToExcel(subjectList, dialog.FileName);
                    MessageBox.Show("Export successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task ImportSubjectAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _excelImporter.ReadSubjectsFromExcel(dialog.FileName);
                    await _courseService.ImportSubjectFromExcel(data);
                    MessageBox.Show("Import successful!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadSubjectAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
