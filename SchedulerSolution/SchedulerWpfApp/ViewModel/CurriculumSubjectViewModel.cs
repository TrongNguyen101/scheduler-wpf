using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumServices;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;
using SchedulerWpfApp.ServiceRefactor.NotificationService;
using Syncfusion.Data;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing CurriculumSubject: loading, importing, exporting, adding, editing, and deleting CurriculumSubjects.
    /// </summary>
    public class CurriculumSubjectViewModel : ViewBaseModel
    {
        #region Fields
        // Dependencies injected via constructor
        private readonly ICurriculumSubjectServices _curriculumSubjectService;
        private readonly ISubjectServices _subjectServices;
        private readonly ICurriculumServices _curriculumServices;
        private readonly INotificationService _notificationService;

        // Internal data fields
        private ObservableCollection<CurriculumSubject> _curriculumSubjects;
        private CurriculumSubject? _selectedCurriculumSubject;
        private string _searchKeyword;
        private bool _isCurriculumSubjectFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isCurriculumSubjectCodeEdit;
        private Subject _selectedSubject;
        private Curriculum _selectedCurriculum;
        private string _selectedSubjectCode;
        private string _selectedCurriculumCode;
        private int _progressValue;
        private bool _isProgressBarOpen;
        private ObservableCollection<CurriculumSubject> _allCurriculumSubjects;
        private ObservableCollection<string> _allSubjectCodes;
        private ObservableCollection<string> _allCurriculumCodes;

        public string Title => SelectedCurriculumSubject?.CurriculumCode != null ? "Chỉnh Sửa Khung Môn" : "Thêm Mới Khung Môn";
        public ObservableCollection<string> TeachingMode { get; set; } = new()  { "ON",
            "OFF",
            "ON/OFF",
            "C-On",
            "EXE",
            "OJT"
        };
        public ObservableCollection<string> PartOfTerm { get; set; } = new()  { "H1.1.5",
            "H1.1.6",
            "H2.6.10",
            "H2.7.12",
            "All"
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Indicates whether the form is in edit mode or add mode.
        /// </summary>
        public bool IsCurriculumSubjectCodeEdit
        {
            get => _isCurriculumSubjectCodeEdit;
            set => SetProperty(ref _isCurriculumSubjectCodeEdit, value);
        }

        /// <summary>
        /// Collection of CurriculumSubjects to be displayed in the UI.
        /// </summary>
        public ObservableCollection<CurriculumSubject> CurriculumSubjects
        {
            get => _curriculumSubjects;
            set => SetProperty(ref _curriculumSubjects, value);
        }

        /// <summary>
        /// Currently selected CurriculumSubject in the UI.
        /// </summary>
        public CurriculumSubject SelectedCurriculumSubject
        {
            get => _selectedCurriculumSubject;
            set
            {
                if (SetProperty(ref _selectedCurriculumSubject, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Indicates whether the CurriculumSubject form is currently open for editing or adding a new CurriculumSubject.
        /// </summary>
        public bool IsCurriculumSubjectFormOpen
        {
            get => _isCurriculumSubjectFormOpen;
            set => SetProperty(ref _isCurriculumSubjectFormOpen, value);
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
        /// Search keyword used to filter the CurriculumSubjects displayed in the UI.
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function filter list by keyword
                    FilterCurriculumSubjects();
                }
            }
        }

        public ObservableCollection<string> AllSubjectCodes
        {
            get => _allSubjectCodes;
            set => SetProperty(ref _allSubjectCodes, value);
        }

        public ObservableCollection<string> AllCurriculumCodes
        {
            get => _allCurriculumCodes;
            set => SetProperty(ref _allCurriculumCodes, value);
        }

        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set => SetProperty(ref _selectedSubject, value);
        }

        public Curriculum SelectedCurriculum
        {
            get => _selectedCurriculum;
            set => SetProperty(ref _selectedCurriculum, value);
        }

        /// <summary>
        /// Selected Subject Code for binding to ComboBox
        /// </summary>
        public string SelectedSubjectCode
        {
            get => _selectedSubjectCode;
            set
            {
                if (SetProperty(ref _selectedSubjectCode, value))
                {
                    // Update subject information when subject code changes
                    _ = UpdateSubjectInfoAsync(value);
                    // Update SelectedCurriculumSubject SubjectCode
                    if (SelectedCurriculumSubject != null)
                    {
                        SelectedCurriculumSubject.SubjectCode = value;
                    }
                }
            }
        }

        /// <summary>
        /// Selected Curriculum Code for binding to ComboBox
        /// </summary>
        public string SelectedCurriculumCode
        {
            get => _selectedCurriculumCode;
            set
            {
                if (SetProperty(ref _selectedCurriculumCode, value))
                {
                    // Update curriculum information when curriculum code changes
                    _ = UpdateCurriculumInfoAsync(value);
                    // Update SelectedCurriculumSubject CurriculumCode
                    if (SelectedCurriculumSubject != null)
                    {
                        SelectedCurriculumSubject.CurriculumCode = value;
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

        // Commands exposed to the View
        public ICommand LoadCurriculumSubjectCommand { get; }
        public ICommand ExportCurriculumSubjectCommand { get; }
        public ICommand ImportCurriculumSubjectCommand { get; }
        public ICommand AddCurriculumSubjectCommand { get; }
        public ICommand EditCurriculumSubjectCommand { get; }
        public ICommand DeleteCurriculumSubjectCommand { get; }
        public ICommand SaveCurriculumSubjectCommand { get; }
        public ICommand CancelEditCurriculumSubjectCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCurriculumSubjectCommand { get; }
        #endregion

        #region Method
        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public CurriculumSubjectViewModel(ICurriculumSubjectServices curriculumSubjectService, ISubjectServices subjectServices, ICurriculumServices curriculumServices, INotificationService notificationService)
        {
            _curriculumSubjectService = curriculumSubjectService;
            _subjectServices = subjectServices;
            _curriculumServices = curriculumServices;
            _notificationService = notificationService;
            CurriculumSubjects = new ObservableCollection<CurriculumSubject>();

            // Initialize commands with async methods
            LoadCurriculumSubjectCommand = new RelayCommand(async () => await LoadCurriculumSubjectAsync());

            ImportCurriculumSubjectCommand = new RelayCommand(async () => await ImportCurriculumSubjectAsync());
            ExportCurriculumSubjectCommand = new RelayCommand(async () => await ExportCurriculumSubjectAsync());

            AddCurriculumSubjectCommand = new RelayCommand(async () => await AddCurriculumSubjectAsync());
            EditCurriculumSubjectCommand = new RelayCommandGeneric<CurriculumSubject>(async (CurriculumSubject) => await EditCurriculumSubjectAsync(CurriculumSubject), (CurriculumSubject) => CurriculumSubject != null);

            // Generic command with parameter (used for deletion)
            DeleteCurriculumSubjectCommand = new RelayCommandGeneric<CurriculumSubject>(async (CurriculumSubject) => await DeleteCurriculumSubjectAsync(CurriculumSubject), (CurriculumSubject) => CurriculumSubject != null);

            SaveCurriculumSubjectCommand = new RelayCommand(async () => await SaveCurriculumSubjectAsync());
            CancelEditCurriculumSubjectCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeleteCurriculumSubjectCommand = new RelayCommand(CancelDelete);

            // Load data immediately when ViewModel is constructed
            _ = LoadCurriculumSubjectAsync();
        }

        /// <summary>
        /// Updates subject information when subject code changes
        /// </summary>
        private async Task UpdateSubjectInfoAsync(string subjectCode)
        {
            if (string.IsNullOrEmpty(subjectCode))
            {
                SelectedSubject = new Subject();
                return;
            }

            try
            {
                var subject = await _subjectServices.GetBySubjectCodeAsync(subjectCode);
                SelectedSubject = subject ?? new Subject();

                // Update SelectedCurriculumSubject with subject information
                if (SelectedCurriculumSubject != null)
                {
                    SelectedCurriculumSubject.SubjectNameEnglish = SelectedSubject.SubjectNameEnglish;
                    SelectedCurriculumSubject.SubjectNameVietnamese = SelectedSubject.SubjectNameVietnamese;
                    // Trigger property change notification for UI update
                    OnPropertyChanged(nameof(SelectedCurriculumSubject));
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi lấy danh sách khung môn.");
            }
        }

        /// <summary>
        /// Updates curriculum information when curriculum code changes
        /// </summary>
        private async Task UpdateCurriculumInfoAsync(string curriculumCode)
        {
            if (string.IsNullOrEmpty(curriculumCode))
            {
                SelectedCurriculum = new Curriculum();
                return;
            }

            try
            {
                var curriculum = await _curriculumServices.GetByCurriculumCodeAsync(curriculumCode);
                SelectedCurriculum = curriculum ?? new Curriculum();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi lấy thông tin khung chương trình.");
            }
        }

        /// <summary>
        /// Loads CurriculumSubject from the data service.
        /// </summary>
        private async Task LoadCurriculumSubjectAsync()
        {
            try
            {
                var curriculumSubjectList = await _curriculumSubjectService.GetAllCurriculumSubjectAsync();
                _allCurriculumSubjects = new ObservableCollection<CurriculumSubject>(curriculumSubjectList);
                ResetToAllCurriculumSubjects();
                var subjectList = await _subjectServices.GetAllAsync();
                var curriculumList = await _curriculumServices.GetAllCurriculumAsync();

                AllSubjectCodes = new ObservableCollection<string>(subjectList.Select(s => s.SubjectCode));
                AllCurriculumCodes = new ObservableCollection<string>(curriculumList.Select(c => c.CurriculumCode));
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi tải danh sách khung môn.");
            }
        }

        /// <summary>
        /// Opens the form to add a new CurriculumSubject.
        /// </summary>
        private async Task AddCurriculumSubjectAsync()
        {
            SelectedCurriculumSubject = new CurriculumSubject(); // Initialize a new curriculumSubject object
            SelectedSubject = new Subject(); // Initialize a new subject object
            SelectedCurriculum = new Curriculum(); // Initialize a new curriculum object
            SelectedSubjectCode = null; // Reset selected codes
            SelectedCurriculumCode = null;
            IsCurriculumSubjectFormOpen = true;
            _isEdit = false;
            IsCurriculumSubjectCodeEdit = false;
        }

        /// <summary>
        /// Opens the form to edit an existing curriculumSubject.
        /// </summary>
        private async Task EditCurriculumSubjectAsync(CurriculumSubject curriculumSubject)
        {
            if (curriculumSubject == null) return;

            SelectedCurriculumSubject = new CurriculumSubject
            {
                Id = curriculumSubject.Id,
                CurriculumCode = curriculumSubject.CurriculumCode,
                SubjectCode = curriculumSubject.SubjectCode,
                SubjectNameEnglish = curriculumSubject.SubjectNameEnglish,
                SubjectNameVietnamese = curriculumSubject.SubjectNameVietnamese,
                TermNo = curriculumSubject.TermNo,
                IsCombo = curriculumSubject.IsCombo,
                Credit = curriculumSubject.Credit,
                TotalSlots = curriculumSubject.TotalSlots,
                TeachingMode = curriculumSubject.TeachingMode,
                PartOfTerm = curriculumSubject.PartOfTerm
            };

            // Set selected codes for ComboBoxes
            SelectedSubjectCode = curriculumSubject.SubjectCode;
            SelectedCurriculumCode = curriculumSubject.CurriculumCode;

            // Load subject and curriculum information
            if (!string.IsNullOrEmpty(curriculumSubject.SubjectCode))
            {
                var subject = await _subjectServices.GetBySubjectCodeAsync(curriculumSubject.SubjectCode);
                SelectedSubject = subject ?? new Subject();
            }

            if (!string.IsNullOrEmpty(curriculumSubject.CurriculumCode))
            {
                var curriculum = await _curriculumServices.GetByCurriculumCodeAsync(curriculumSubject.CurriculumCode);
                SelectedCurriculum = curriculum ?? new Curriculum();
            }

            IsCurriculumSubjectFormOpen = true;
            _isEdit = true;
            IsCurriculumSubjectCodeEdit = true;
        }

        /// <summary>
        /// Opens the form to edit an existing curriculumSubject.
        /// </summary>
        /// <param name="CurriculumSubject"></param>
        private async Task DeleteCurriculumSubjectAsync(CurriculumSubject curriculumSubject)
        {
            if (curriculumSubject == null) return;

            SelectedCurriculumSubject = curriculumSubject;
            IsOpenDialog = true;
        }

        /// <summary>
        /// Saves the current curriculumSubject to the data source, either creating a new one or updating an existing one.
        /// </summary>
        private async Task SaveCurriculumSubjectAsync()
        {
            if (SelectedCurriculumSubject == null)
                return;

            // Validate the curriculumSubject before saving
            if (string.IsNullOrWhiteSpace(SelectedCurriculumSubject.CurriculumCode) || string.IsNullOrWhiteSpace(SelectedCurriculumSubject.SubjectCode) || !SelectedCurriculumSubject.Credit.HasValue || !SelectedCurriculumSubject.TotalSlots.HasValue || string.IsNullOrEmpty(SelectedCurriculumSubject.PartOfTerm) || string.IsNullOrEmpty(SelectedCurriculumSubject.TeachingMode))
            {
                _notificationService.ShowWarning("Vui lòng điền đầy đủ thông tin khung môn.");
                // Open the curriculumSubject form for user to fill in the details
                IsCurriculumSubjectFormOpen = true;
                return;
            }
            if (SelectedCurriculumSubject.TermNo < 1 || SelectedCurriculumSubject.TermNo > 9)
            {
                _notificationService.ShowWarning("Số học kỳ phải lớn hơn 1 hoặc nhỏ hơn 9.");
                IsCurriculumSubjectFormOpen = true;
                return;
            }
            if (SelectedCurriculumSubject.TotalSlots <= 0 || SelectedCurriculumSubject.Credit <= 0)
            {
                _notificationService.ShowWarning("Số tín chỉ và tổng số giờ học phải lớn hơn 0");
                IsCurriculumSubjectFormOpen = true;
                return;
            }

            try
            {
                // Check if _isEdit is false will create new. Otherwise update
                if (!_isEdit)
                {
                    // Check if the curriculumSubject already exists
                    var existingCurriculumSubject = CurriculumSubjects.Any(s => s.CurriculumCode == SelectedCurriculumSubject.CurriculumCode && s.SubjectCode == SelectedCurriculumSubject.SubjectCode && s.TermNo == SelectedCurriculumSubject.TermNo);

                    if (!existingCurriculumSubject)
                    {
                        // Add new curriculumSubject
                        await _curriculumSubjectService.AddCurriculumSubject(SelectedCurriculumSubject);
                        _notificationService.ShowSuccess("Thêm khung môn thành công.");
                    }
                    else
                        // Warning
                        _notificationService.ShowWarning("Khung môn đã tồn tại. Vui lòng kiểm tra lại thông tin khung môn.");
                }
                else
                {
                    // Check if the curriculumSubject exists
                    var checkCurriculumSubject = CurriculumSubjects.FirstOrDefault(s => s.CurriculumCode == SelectedCurriculumSubject.CurriculumCode && s.SubjectCode == SelectedCurriculumSubject.SubjectCode && s.TermNo == SelectedCurriculumSubject.TermNo && s.Id != SelectedCurriculumSubject.Id);
                    if (checkCurriculumSubject != null)
                    {
                        // Warning
                        _notificationService.ShowWarning("Khung môn đã tồn tại. Vui lòng kiểm tra lại thông tin khung môn.");
                        return;
                    }

                    var existingCurriculumSubject = CurriculumSubjects.FirstOrDefault(s => s.Id == SelectedCurriculumSubject.Id);

                    if (existingCurriculumSubject != null)
                    {
                        existingCurriculumSubject.CurriculumCode = SelectedCurriculumSubject.CurriculumCode;
                        existingCurriculumSubject.SubjectCode = SelectedCurriculumSubject.SubjectCode;
                        existingCurriculumSubject.SubjectNameEnglish = SelectedCurriculumSubject.SubjectNameEnglish;
                        existingCurriculumSubject.SubjectNameVietnamese = SelectedCurriculumSubject.SubjectNameVietnamese;
                        existingCurriculumSubject.TermNo = SelectedCurriculumSubject.TermNo;
                        existingCurriculumSubject.IsCombo = SelectedCurriculumSubject.IsCombo;
                        existingCurriculumSubject.Credit = SelectedCurriculumSubject.Credit;
                        existingCurriculumSubject.TotalSlots = SelectedCurriculumSubject.TotalSlots;
                        existingCurriculumSubject.TeachingMode = SelectedCurriculumSubject.TeachingMode;
                        existingCurriculumSubject.PartOfTerm = SelectedCurriculumSubject.PartOfTerm;

                        // Update the curriculumSubject in the data source
                        await _curriculumSubjectService.UpdateCurriculumSubject(existingCurriculumSubject);
                        _notificationService.ShowSuccess("Cập nhật khung môn thành công.");
                    }
                    else
                    {
                        // Warning
                        _notificationService.ShowWarning("Khung môn không tồn tại. Vui lòng kiểm tra lại thông tin khung môn.");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi lưu khung môn.");
            }
            finally
            {
                // Close form  reset
                IsCurriculumSubjectFormOpen = false;
                SelectedCurriculumSubject = null;
                SelectedSubjectCode = string.Empty;
                SelectedCurriculumCode = string.Empty;
                LoadCurriculumSubjectAsync();
            }
        }

        /// <summary>
        /// Cancels the current edit operation and closes the CurriculumSubject form.
        /// </summary>
        public void CancelEdit()
        {
            IsCurriculumSubjectFormOpen = false;
            SelectedCurriculumSubject = null;
            SelectedSubjectCode = string.Empty;
            SelectedCurriculumCode = string.Empty;
        }

        /// <summary>
        /// Cancels the delete operation and closes the confirmation dialog.
        /// </summary>
        public void CancelDelete()
        {
            IsOpenDialog = false;
        }

        /// <summary>
        /// Confirms the deletion of the selected curriculumSubject and removes it from the data source.
        /// </summary>
        private async Task ConfirmDeleteAsync()
        {
            if (SelectedCurriculumSubject == null)
            {
                _notificationService.ShowWarning("Không có khung môn nào được chọn để xóa.");
                IsOpenDialog = false;
                return;
            }

            try
            {
                // Delete the selected curriculumSubject from the data source
                await _curriculumSubjectService.DeleteCurriculumSubject(SelectedCurriculumSubject.Id);

                _notificationService.ShowSuccess("Xóa khung môn thành công.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Xóa khung môn thất bại.");
            }
            finally
            {
                IsOpenDialog = false;
                SelectedCurriculumSubject = null;
                LoadCurriculumSubjectAsync();
            }
        }

        /// <summary>
        /// Exports the current list of curriculumSubjects to an Excel file.
        /// </summary>
        private async Task ExportCurriculumSubjectAsync()
        {
            if (_allCurriculumSubjects == null || _allCurriculumSubjects.Count == 0)
            {
                _notificationService.ShowWarning("Không có khung môn nào để xuất.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "CurriculumSubjects.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var curriculumSubjectList = _allCurriculumSubjects.Where(p => p != null).ToList();
                    // Use the Excel exporter service to export the CurriculumSubjects to the selected file
                    _curriculumSubjectService.ExportToExcel(curriculumSubjectList, dialog.FileName);
                    _notificationService.ShowSuccess("Xuất khung môn thành công.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError("Xuất khung môn thất bại.");
                }
            }
        }

        /// <summary>
        /// Imports curriculumSubjects from an Excel file and adds them to the data source.
        /// </summary>
        private async Task ImportCurriculumSubjectAsync()
        {
            var curriculums = await _curriculumServices.GetAllCurriculumAsync();
            var subjects = await _subjectServices.GetAllAsync();

            // Check if lecturers and subjects lists are empty before proceeding with import
            if (!curriculums.Any() && !subjects.Any())
            {
                _notificationService.ShowWarning("Danh sách khung chương trình và môn học đều đang trống. Vui lòng thêm danh sách khung chương trình và môn học trước");
                return;
            }
            else if (!curriculums.Any())
            {
                _notificationService.ShowWarning("Không có khung chương trình nào trong hệ thống. Vui lòng thêm danh sách khung chương trình trước");
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
                    var data = _curriculumSubjectService.ReadCurriculumSubjectsFromExcel(dialog.FileName);

                    IsProgressBarOpen = true;
                    await _curriculumSubjectService.ImportCurriculumSubjectFromExcel(data, progress);
                    IsProgressBarOpen = false;

                    _notificationService.ShowSuccess("Nhập khung môn thành công.");
                    await LoadCurriculumSubjectAsync();
                }
                catch (Exception ex)
                {
                    IsProgressBarOpen = false;
                    _notificationService.ShowError("Nhập khung môn thất bại.");
                }
            }
        }

        /// <summary>
        /// Filter curriculumSubject with 3 column 
        /// </summary>
        private void FilterCurriculumSubjects()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // If search keyword is empty, reset to all CurriculumSubjects
                ResetToAllCurriculumSubjects();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by CurriculumSubjectCode
                var filtered = _allCurriculumSubjects.Where(CurriculumSubject =>
                (!string.IsNullOrEmpty(CurriculumSubject.CurriculumCode) && CurriculumSubject.CurriculumCode.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(CurriculumSubject.SubjectCode) && CurriculumSubject.SubjectCode.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(CurriculumSubject.SubjectNameEnglish) && CurriculumSubject.SubjectNameEnglish.ToLower().Contains(lowerKeyword)) ||
                (!string.IsNullOrEmpty(CurriculumSubject.SubjectNameVietnamese) && CurriculumSubject.SubjectNameVietnamese?.ToLower().Contains(lowerKeyword) == true)
                ).ToList();

                CurriculumSubjects = new ObservableCollection<CurriculumSubject>(filtered);
            }
        }

        /// <summary>
        /// Reset CurriculumSubjects
        /// </summary>
        private void ResetToAllCurriculumSubjects()
        {
            CurriculumSubjects = new ObservableCollection<CurriculumSubject>(_allCurriculumSubjects);
        }
    }
    #endregion
}