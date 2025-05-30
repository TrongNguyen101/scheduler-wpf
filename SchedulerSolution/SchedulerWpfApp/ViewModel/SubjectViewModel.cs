using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing subjects: loading, importing, exporting, adding, editing, and deleting subjects.
    /// </summary>
    public class SubjectViewModel : ViewBaseModel
    {
        #region Fields
        // Dependencies injected via constructor
        private readonly ISubjectServices _courseService;
        private readonly IExcelSubjectImporter _excelImporter;
        private readonly IExcelSubjectExporter _excelExporter;

        // Internal data fields
        private ObservableCollection<Subject> _subject;
        private Subject? _selectedSubject;
        private string _searchKeyword;
        private bool _isSubjectFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isSubjectCodeEdit;
        private ObservableCollection<Subject> _allSubjects;
        public string Title => SelectedSubject?.SubjectCode != null ? "Chỉnh Sửa Môn Học" : "Thêm Mới Môn Học";
        #endregion

        #region Constructor
        /// <summary>
        /// Indicates whether the form is in edit mode or add mode.
        /// </summary>
        public bool IsSubjectCodeEdit
        {
            get => _isSubjectCodeEdit;
            set => SetProperty(ref _isSubjectCodeEdit, value);
        }

        /// <summary>
        /// Collection of subjects to be displayed in the UI.
        /// </summary>
        public ObservableCollection<Subject> Subjects
        {
            get => _subject;
            set => SetProperty(ref _subject, value);
        }

        /// <summary>
        /// Currently selected subject in the UI.
        /// </summary>
        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetProperty(ref _selectedSubject, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Indicates whether the subject form is currently open for editing or adding a new subject.
        /// </summary>
        public bool IsSubjectFormOpen
        {
            get => _isSubjectFormOpen;
            set => SetProperty(ref _isSubjectFormOpen, value);
        }

        /// <summary>
        /// Indicates whether the confirmation dialog for deletion is open.
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
        /// Search keyword used to filter the subjects displayed in the UI.
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function fillter list by keyword
                    FilterSubjects();
                }
            }
        }
        // Commands exposed to the View
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

        #endregion

        #region Method
        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public SubjectViewModel(ISubjectServices courseService, IExcelSubjectExporter excelExporter, IExcelSubjectImporter excelImporter)
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

        /// <summary>
        /// Loads subject from the data service and populates the Lecture collection.
        /// </summary>
        private async Task LoadSubjectAsync()
        {
            try
            {
                var subjectList = await _courseService.GetAllAsync();
                _allSubjects = new ObservableCollection<Subject>(subjectList);
                ResetToAllSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load subjects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the form to add a new subject.
        /// </summary>
        private async Task AddSubjectAsync()
        {
            SelectedSubject = new Subject(); // Khởi tạo object trống cho form
            IsSubjectFormOpen = true;
            _isEdit = false;
            IsSubjectCodeEdit = false;
        }

        /// <summary>
        /// Opens the form to edit an existing subject.
        /// </summary>
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
            _isEdit = true;
            IsSubjectCodeEdit = true;
        }

        /// <summary>
        /// Opens the form to edit an existing subject.
        /// </summary>
        /// <param name="subject"></param>
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

        /// <summary>
        /// Saves the current subject to the data source, either creating a new one or updating an existing one.
        /// </summary>
        private async Task SaveSubjectAsync()
        {
            if (SelectedSubject == null)
                return;
            if (string.IsNullOrWhiteSpace(SelectedSubject.SubjectCode) ||
                string.IsNullOrWhiteSpace(SelectedSubject.SubjectName) ||
                string.IsNullOrWhiteSpace(SelectedSubject.Major) ||
                SelectedSubject.TotalSessions == 0 ||
                SelectedSubject.SlotsPerWeek == 0)
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin môn học.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedSubject.SlotsPerWeek <= 0 || SelectedSubject.TotalSessions <= 0)
            {
                MessageBox.Show("Số buổi học trong tuần và tổng số buổi học phải lớn hơn 0.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // Check if _isEdit is false will create new course. Otherwise, update course
                if (!_isEdit)
                {
                    var existingSubject = Subjects.FirstOrDefault(s => s.SubjectCode == SelectedSubject.SubjectCode);

                    if (existingSubject == null)
                    {
                        await _courseService.AddSubject(SelectedSubject);
                        MessageBox.Show("Thêm môn học thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        // Cảnh báo
                        MessageBox.Show("Môn học đã tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
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
                        MessageBox.Show("Update môn học thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Cảnh báo
                        MessageBox.Show("Môn học không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

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

        /// <summary>
        /// Cancels the current edit operation and closes the subject form.
        /// </summary>
        public void CancelEdit()
        {
            IsSubjectFormOpen = false;
            SelectedSubject = null;
        }

        /// <summary>
        /// Cancels the delete operation and closes the confirmation dialog.
        /// </summary>
        public void CancelDelete()
        {
            IsOpenDialog = false;
        }

        /// <summary>
        /// Confirms the deletion of the selected subject and removes it from the data source.
        /// </summary>
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

        /// <summary>
        /// Exports the current list of subjects to an Excel file.
        /// </summary>
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

        /// <summary>
        /// Imports subjects from an Excel file and adds them to the data source.
        /// </summary>
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

        /// <summary>
        /// Filter subject with 3 column SubjectCode, SubjectName, Major
        /// </summary>
        private void FilterSubjects()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                ResetToAllSubjects();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by ClassId, Category, Major
                var filtered = _allSubjects.Where(subject =>
                    (!string.IsNullOrEmpty(subject.SubjectCode) && subject.SubjectCode.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(subject.SubjectName) && subject.SubjectName.ToLower().Contains(lowerKeyword)) ||
                    (!string.IsNullOrEmpty(subject.Major) && subject.Major.ToLower().Contains(lowerKeyword))
                ).ToList();

                Subjects = new ObservableCollection<Subject>(filtered);
            }
        }

        /// <summary>
        /// Reset Subjects
        /// </summary>
        private void ResetToAllSubjects()
        {
            Subjects = new ObservableCollection<Subject>(_allSubjects);
        }
    }
    #endregion
}

