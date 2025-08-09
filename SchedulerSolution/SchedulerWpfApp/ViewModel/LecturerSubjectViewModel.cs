using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.Model;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SchedulerWpfApp.Helper;
using Microsoft.Win32;
using System.Windows;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.NotificationService;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel for managing lecturer subjects, including adding, editing, deleting, importing, and exporting subjects.
    /// </summary>
    public class LecturerSubjectViewModel : ViewBaseModel
    {
        #region Fields
        private readonly ILecturerSubjectServices _lecturerSubjectService;
        private readonly ISubjectServices _subjectServices;
        private readonly ILecturerServices _lectureService;
        private readonly INotificationService _notificationService;

        private ObservableCollection<LecturerSubject> _lecturersubject;
        private LecturerSubject? _selectedSubject;
        public string FormTitle => SelectedLecturerSubject?.Id == 0 ? "Thêm môn mới cho giảng viên" : "Chỉnh sửa môn cho giảng viên";
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEditing;
        private bool _isLecturerSubjectOpen;
        private int _progressValue;
        private bool _isProgressBarOpen;
        private string _searchKeyword;
        private ObservableCollection<LecturerSubject> _allLecturerSubject;
        #endregion

        #region Construsctor
        /// <summary>
        /// Gets or sets the collection of lecture subjects displayed in the UI.
        /// </summary>
        public ObservableCollection<LecturerSubject> LecturerSubjects
        {
            get => _lecturersubject;
            set => SetProperty(ref _lecturersubject, value);
        }

        /// <summary>
        /// Search keyword, triggers filtering when updated
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function fillter list by keyword
                    FilterLecturerSubject();
                }
            }
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
        public bool IsLecturerSubjectFormOpen
        {
            get => _isLecturerSubjectOpen;
            set => SetProperty(ref _isLecturerSubjectOpen, value);
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
        public LecturerSubjectViewModel(ILecturerSubjectServices lectureSubjectService, ISubjectServices subjectServices, ILecturerServices lecturerServices, INotificationService notificationService)
        {
            _lecturerSubjectService = lectureSubjectService;
            _subjectServices = subjectServices;
            _lectureService = lecturerServices;
            _notificationService = notificationService;
            LecturerSubjects = new ObservableCollection<LecturerSubject>();
            ImportLectureSubjectCommand = new RelayCommand(async () => await ImportLecturerSubjectListAsync());
            ExportLectureSubjectCommand = new RelayCommand(async () => await ExportLecturerSubjectAsync());
            EditLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await EditLecturerSubject(lectursubject));
            AddLectureSubjectCommand = new RelayCommand(async () => await AddLecturerSubjectAsync());
            DeleteLectureSubjectCommand = new RelayCommandGeneric<LecturerSubject>(async (lectursubject) => await DeleteLecturerSubjectAsync(lectursubject));
            CancelEditLectureSubjectCommand = new RelayCommand(CancelEdit);
            CancelDeleteLectureSubjectCommand = new RelayCommand(CancelDelete);
            SaveLectureSubjectCommand = new RelayCommand(async () => await SaveLecturerSubjectAsync());
            ConfirmDeleteLectureSubjectCommand = new RelayCommand(async () => await ConfirmDeleteLecturerSubjectAsync());
            SelectedLecturerSubject = new LecturerSubject();

            _ = LoadLecturerSubjects();
        }
        /// <summary>
        /// Asynchronously loads all lecture subjects from the service and populates the LectureSubjects collection.
        /// </summary>
        private async Task LoadLecturerSubjects()
        {
            try
            {
                var lecturesubjects = await _lecturerSubjectService.GetAllAsync();
                _allLecturerSubject = new ObservableCollection<LecturerSubject>(lecturesubjects);
                ResetToAllLecturerSubject();
                var subjectList = await _subjectServices.GetAllAsync();
                var lecturerlist = await _lectureService.GetAllLecturerAsync();
                Subjects = new ObservableCollection<Subject>(subjectList);
                Lecturers = new ObservableCollection<Lecturer>(lecturerlist);
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lấy danh sách phân công giảng dạy cho giảng viên không thành công");
            }
        }

        /// <summary>
        /// Imports lecture subjects from an Excel file using the Excel importer service.
        /// </summary>
        private async Task ImportLecturerSubjectListAsync()
        {
            var lecturers = await _lectureService.GetAllLecturerAsync();
            var subjects = await _subjectServices.GetAllAsync();

            // Check if lecturers and subjects lists are empty before proceeding with import
            if (!lecturers.Any() && !subjects.Any())
            {
                _notificationService.ShowWarning("Danh sách giảng viên và môn học đều đang trống. Vui lòng thêm danh sách giảng viên và môn học trước");
                return;
            }
            else if (!lecturers.Any())
            {
                _notificationService.ShowWarning("Không có giảng viên nào trong hệ thống. Vui lòng thêm danh sách giảng viên trước");
                return;
            }
            else if (!subjects.Any())
            {
                _notificationService.ShowWarning("Không có môn học nào trong hệ thống. Vui lòng thêm danh sách môn học trước");
                return;
            }

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
                    // call ReadLectureSubjectFromExcel function to process file and read file when importing
                    var data = _lecturerSubjectService.ReadLecturerSubjectFromExcel(dialog.FileName);
                    // call ImportLecturerSubjectFromExcel function to add new data to database
                    IsProgressBarOpen = true;
                    await _lecturerSubjectService.ImportLecturerSubjectFromExcel(data, progress);
                    IsProgressBarOpen = false;
                    _notificationService.ShowSuccess("Thêm mới danh sách phân công giảng dạy cho giảng viên thành công!");
                    await LoadLecturerSubjects();
                }
                catch (Exception)
                {
                    IsProgressBarOpen = false;
                    _notificationService.ShowError($"Thêm mới danh sách phân công giảng dạy cho giảng viên không thành công.");
                }
            }
        }

        /// <summary>
        /// Exports the list of lecture subjects to an Excel file using the Excel exporter service.
        /// </summary>
        private async Task ExportLecturerSubjectAsync()
        {
            if (LecturerSubjects == null || LecturerSubjects.Count == 0)
            {
                _notificationService.ShowWarning("Không có phân công giảng dạy nào để xuất.");
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
                    //Filter the LecturerSubjects list to remove null elements
                    var lectureSubjectList = await _lecturerSubjectService.GetAllAsync();
                    // call ExportToLectureSubjectExcel function to export file
                    _lecturerSubjectService.ExportToLecturerSubjectExcel(lectureSubjectList, dialog.FileName);
                    _notificationService.ShowSuccess("Xuất danh sách phân công giảng dạy cho giảng viên thành công!");
                }
                catch (Exception)
                {
                    _notificationService.ShowError($"Xuất danh sách phân công giảng dạy cho giảng viên không thành công.");
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
            IsLecturerSubjectFormOpen = true;
            _isEditing = true;
        }

        /// <summary>
        /// Opens the form to add a new lecturer subject.
        /// </summary>
        /// <returns></returns>
        public async Task AddLecturerSubjectAsync()
        {
            IsLecturerSubjectFormOpen = true;
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
            // set SelectedLecturerSubject null 
            SelectedLecturerSubject = null;
            IsOpenDialog = false;
        }

        /// <summary>
        /// Cancels the edit operation and closes the lecture subject form.
        /// </summary>
        private void CancelEdit()
        {
            // set SelectedLecturerSubject null 
            SelectedLecturerSubject = new LecturerSubject();
            IsLecturerSubjectFormOpen = false;
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
                !SelectedLecturerSubject.Term.HasValue ||
                string.IsNullOrWhiteSpace(SelectedLecturerSubject.Major))
            {
                _notificationService.ShowWarning("Dữ liệu không được để trống.");
                IsLecturerSubjectFormOpen = true;
                return;
            }
            if (SelectedLecturerSubject.NumberOfClasses == null || SelectedLecturerSubject.NumberOfClasses <= 0)
            {
                _notificationService.ShowWarning("Số lượng lớp học lớn hơn 0.");
                IsLecturerSubjectFormOpen = true;
                return;
            }
            if (SelectedLecturerSubject.Term <= 0 || SelectedLecturerSubject.Term > 9)
            {
                _notificationService.ShowWarning("Dữ liệu kỳ phải lớn hơn 0 và bé hơn 9.");
                IsLecturerSubjectFormOpen = true;
                return;
            }
            if (SelectedLecturerSubject.TotalSlots == null || SelectedLecturerSubject.TotalSlots <= 0)
            {
                _notificationService.ShowWarning("Dữ liệu tổng slot phải lớn hơn 0");
                IsLecturerSubjectFormOpen = true;
                return;
            }
            try
            {
                var existingLecturerSubject = await _lecturerSubjectService.CheckLecturerSubjectExits(SelectedLecturerSubject);
                if (_isEditing)
                {
                    if (existingLecturerSubject != null)
                    {
                        _notificationService.ShowWarning("Lịch phân công cho giáo viên này đã bị trùng.");
                        IsLecturerSubjectFormOpen = true;
                        return;
                    }
                    await _lecturerSubjectService.UpdateAsync(SelectedLecturerSubject);
                    _notificationService.ShowSuccess("Cập nhật lịch phân công giảng dạy cho giảng viên thành công!");
                }
                else
                {
                    if (existingLecturerSubject != null)
                    {
                        _notificationService.ShowWarning("Lịch phân công cho giáo viên này đã bị trùng.");
                        IsLecturerSubjectFormOpen = true;
                        return;
                    }
                    // Add new lecturer subject
                    await _lecturerSubjectService.AddAsync(SelectedLecturerSubject);
                    _notificationService.ShowSuccess("Thêm mới lịch phân công giảng dạy cho giảng viên thành công!");
                }
                IsLecturerSubjectFormOpen = false;
                SelectedLecturerSubject = new LecturerSubject();

                await LoadLecturerSubjects();
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Lưu lịch phân công giảng dạy cho giảng viên không thành công.");
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
                await _lecturerSubjectService.DeleteAsync(SelectedLecturerSubject.Id);
                _notificationService.ShowSuccess("Xóa lịch phân công giảng dạy cho giảng viên thành công!");
                IsOpenDialog = false;
                await LoadLecturerSubjects();
            }
            catch (Exception)
            {
                _notificationService.ShowError($"Xóa lịch phân công giảng dạy cho giảng viên không thành công");
            }
        }

        /// <summary>
        /// Filters the LecturerSubject collection based on the search keyword.
        /// If the search keyword is empty, it resets to show all LecturerSubject.
        /// </summary>
        private void FilterLecturerSubject()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // If search keyword is empty, reset to all LecturerSubject
                ResetToAllLecturerSubject();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by LecturerSubjectcode
                var filtered = _allLecturerSubject.Where(LecturerSubject =>
                (!string.IsNullOrEmpty(LecturerSubject.LecturerId) && LecturerSubject.LecturerId.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(LecturerSubject.LecturerName) && LecturerSubject.LecturerName.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(LecturerSubject.SubjectCode) && LecturerSubject.SubjectCode.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(LecturerSubject.SubjectName) && LecturerSubject.SubjectName.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(LecturerSubject.Major) && LecturerSubject.Major.ToLower().Contains(lowerKeyword)) ||
                (int.TryParse(SearchKeyword, out int floorKeyword) && LecturerSubject.Term == floorKeyword)
                ).ToList();
                LecturerSubjects = new ObservableCollection<LecturerSubject>(filtered);
            }
        }

        /// <summary>
        /// Reset LecturerSubject
        /// </summary>
        private void ResetToAllLecturerSubject()
        {
            LecturerSubjects = new ObservableCollection<LecturerSubject>(_allLecturerSubject);
        }
    }
    #endregion
}
