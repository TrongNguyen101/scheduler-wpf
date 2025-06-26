using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumServices;

namespace SchedulerWpfApp.ViewModel
{
    /// <summary>
    /// ViewModel responsible for managing curriculum: loading, importing, exporting, adding, editing, and deleting curriculums.
    /// </summary>
    public class CurriculumViewModel : ViewBaseModel
    {
        #region Fields
        // Dependencies injected via constructor
        private readonly ICurriculumServices _curriculumService;

        // Internal data fields
        private ObservableCollection<Curriculum> _curriculums;
        private Curriculum? _selectedCurriculum;
        private string _searchKeyword;
        private bool _isCurriculumFormOpen;
        private bool _isOpenDialog;
        private bool _isConfirmationOpen;
        private bool _isEdit;
        private bool _isCurriculumCodeEdit;
        private ObservableCollection<Curriculum> _allCurriculums;
        public string Title => SelectedCurriculum?.CurriculumCode != null ? "Chỉnh Sửa Khung Chương Trình" : "Thêm Mới Khung Chương Trình";
        #endregion

        #region Constructor
        /// <summary>
        /// Indicates whether the form is in edit mode or add mode.
        /// </summary>
        public bool IsCurriculumCodeEdit
        {
            get => _isCurriculumCodeEdit;
            set => SetProperty(ref _isCurriculumCodeEdit, value);
        }

        /// <summary>
        /// Collection of curriculums to be displayed in the UI.
        /// </summary>
        public ObservableCollection<Curriculum> Curriculums
        {
            get => _curriculums;
            set => SetProperty(ref _curriculums, value);
        }

        /// <summary>
        /// Currently selected curriculum in the UI.
        /// </summary>
        public Curriculum SelectedCurriculum
        {
            get => _selectedCurriculum;
            set
            {
                if (SetProperty(ref _selectedCurriculum, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Indicates whether the curriculum form is currently open for editing or adding a new curriculum.
        /// </summary>
        public bool IsCurriculumFormOpen
        {
            get => _isCurriculumFormOpen;
            set => SetProperty(ref _isCurriculumFormOpen, value);
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
        /// Search keyword used to filter the curriculums displayed in the UI.
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    // use function filter list by keyword
                    FilterCurriculums();
                }
            }
        }
        // Commands exposed to the View
        public ICommand LoadCurriculumCommand { get; }
        public ICommand ExportCurriculumCommand { get; }
        public ICommand ImportCurriculumCommand { get; }
        public ICommand AddCurriculumCommand { get; }
        public ICommand EditCurriculumCommand { get; }
        public ICommand DeleteCurriculumCommand { get; }
        public ICommand SaveCurriculumCommand { get; }
        public ICommand CancelEditCurriculumCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCurriculumCommand { get; }
        #endregion

        #region Method
        /// <summary>
        /// Constructor initializes dependencies and commands.
        /// </summary>
        public CurriculumViewModel(ICurriculumServices curriculumService)
        {
            _curriculumService = curriculumService;

            Curriculums = new ObservableCollection<Curriculum>();

            // Initialize commands with async methods
            LoadCurriculumCommand = new RelayCommand(async () => await LoadCurriculumAsync());

            ImportCurriculumCommand = new RelayCommand(async () => await ImportCurriculumAsync());
            ExportCurriculumCommand = new RelayCommand(async () => await ExportCurriculumAsync());

            AddCurriculumCommand = new RelayCommand(async () => await AddCurriculumAsync());
            EditCurriculumCommand = new RelayCommandGeneric<Curriculum>(async (Curriculum) => await EditCurriculumAsync(Curriculum), (Curriculum) => Curriculum != null);

            // Generic command with parameter (used for deletion)
            DeleteCurriculumCommand = new RelayCommandGeneric<Curriculum>(async (Curriculum) => await DeleteCurriculumAsync(Curriculum), (Curriculum) => Curriculum != null);

            SaveCurriculumCommand = new RelayCommand(async () => await SaveCurriculumAsync());
            CancelEditCurriculumCommand = new RelayCommand(CancelEdit);
            ConfirmDeleteCommand = new RelayCommand(async () => await ConfirmDeleteAsync());
            CancelDeleteCurriculumCommand = new RelayCommand(CancelDelete);

            // Load data immediately when ViewModel is constructed
            _ = LoadCurriculumAsync();
        }

        /// <summary>
        /// Loads curriculum from the data service.
        /// </summary>
        private async Task LoadCurriculumAsync()
        {
            try
            {
                var curriculumList = await _curriculumService.GetAllCurriculumAsync();
                _allCurriculums = new ObservableCollection<Curriculum>(curriculumList);
                ResetToAllCurriculums();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Curriculums: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Opens the form to add a new curriculum.
        /// </summary>
        private async Task AddCurriculumAsync()
        {
            SelectedCurriculum = new Curriculum(); // Initialize a new curriculum object
            IsCurriculumFormOpen = true;
            _isEdit = false;
            IsCurriculumCodeEdit = false;
        }

        /// <summary>
        /// Opens the form to edit an existing curriculum.
        /// </summary>
        private async Task EditCurriculumAsync(Curriculum curriculum)
        {
            if (curriculum == null) return;

            SelectedCurriculum = new Curriculum
            {
                CurriculumCode = curriculum.CurriculumCode,
                IsActive = curriculum.IsActive,
            };
            IsCurriculumFormOpen = true;
            _isEdit = true;
            IsCurriculumCodeEdit = true;
        }

        /// <summary>
        /// Opens the form to edit an existing curriculum.
        /// </summary>
        /// <param name="Curriculum"></param>
        private async Task DeleteCurriculumAsync(Curriculum curriculum)
        {
            if (curriculum == null) return;

            SelectedCurriculum = curriculum;
            IsOpenDialog = true;
        }

        /// <summary>
        /// Saves the current curriculum to the data source, either creating a new one or updating an existing one.
        /// </summary>
        private async Task SaveCurriculumAsync()
        {
            if (SelectedCurriculum == null)
                return;

            // Validate the curriculum before saving
            if (string.IsNullOrWhiteSpace(SelectedCurriculum.CurriculumCode))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin khung chương trình.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Open the curriculum form for user to fill in the details
                IsCurriculumFormOpen = true;
                return;
            }

            try
            {
                // Check if _isEdit is false will create new. Otherwise update
                if (!_isEdit)
                {
                    // Check if the curriculum already exists
                    var existingCurriculum = Curriculums.FirstOrDefault(s => s.CurriculumCode == SelectedCurriculum.CurriculumCode);

                    if (existingCurriculum == null)
                    {
                        // Add new curriculum
                        await _curriculumService.AddCurriculum(SelectedCurriculum);
                        MessageBox.Show("Thêm khung chương trình thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        // Warning
                        MessageBox.Show("Khung chương trình đã tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Check if the curriculum exists for update
                    var existingCurriculum = Curriculums.FirstOrDefault(s => s.CurriculumCode == SelectedCurriculum.CurriculumCode);

                    if (existingCurriculum != null)
                    {
                        existingCurriculum.IsActive = SelectedCurriculum.IsActive;

                        // Update the curriculum in the data source
                        await _curriculumService.UpdateCurriculum(existingCurriculum);
                        MessageBox.Show("Update khung chương trình thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Warning
                        MessageBox.Show("Khung chương trình không tồn tại", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lưu khung chương trình thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Close form  reset
                IsCurriculumFormOpen = false;
                SelectedCurriculum = null;
                LoadCurriculumAsync();
            }
        }

        /// <summary>
        /// Cancels the current edit operation and closes the curriculum form.
        /// </summary>
        public void CancelEdit()
        {
            IsCurriculumFormOpen = false;
            SelectedCurriculum = null;
        }

        /// <summary>
        /// Cancels the delete operation and closes the confirmation dialog.
        /// </summary>
        public void CancelDelete()
        {
            IsOpenDialog = false;
        }

        /// <summary>
        /// Confirms the deletion of the selected curriculum and removes it from the data source.
        /// </summary>
        private async Task ConfirmDeleteAsync()
        {
            if (SelectedCurriculum == null)
            {
                MessageBox.Show("Không có khung chương trình nào được chọn để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsOpenDialog = false;
                return;
            }

            try
            {
                // Delete the selected curriculum from the data source
                await _curriculumService.DeleteCurriculum(SelectedCurriculum.CurriculumCode);

                MessageBox.Show("Xóa khung chương trình thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xóa khung chương trình thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsOpenDialog = false;
                SelectedCurriculum = null;
                LoadCurriculumAsync();
            }
        }

        /// <summary>
        /// Exports the current list of curriculums to an Excel file.
        /// </summary>
        private async Task ExportCurriculumAsync()
        {
            if (_allCurriculums == null || _allCurriculums.Count == 0)
            {
                MessageBox.Show("Không có khung chương trình nào để xóa.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = "Curriculums.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Export only non-null list
                    var curriculumList = _allCurriculums.Where(p => p != null).ToList();
                    // Use the Excel exporter service to export the curriculums to the selected file
                    _curriculumService.ExportToExcel(curriculumList, dialog.FileName);
                    MessageBox.Show("Export thành công!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export thất bại: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Imports curriculums from an Excel file and adds them to the data source.
        /// </summary>
        private async Task ImportCurriculumAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _curriculumService.ReadCurriculumsFromExcel(dialog.FileName);
                    // Validate the imported data
                    await _curriculumService.ImportCurriculumFromExcel(data);
                    MessageBox.Show("Import thành công!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCurriculumAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import thất bại: {ex.InnerException.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Filter curriculum with 3 column curriculumCode
        /// </summary>
        private void FilterCurriculums()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // If search keyword is empty, reset to all Curriculums
                ResetToAllCurriculums();
            }
            else
            {
                // enter keyword from box
                var lowerKeyword = SearchKeyword.ToLower();
                // can search by CurriculumCode
                var filtered = _allCurriculums.Where(Curriculum =>
                (!string.IsNullOrEmpty(Curriculum.CurriculumCode) && Curriculum.CurriculumCode.ToLower().Contains(lowerKeyword))).ToList();

                Curriculums = new ObservableCollection<Curriculum>(filtered);
            }
        }

        /// <summary>
        /// Reset curriculums
        /// </summary>
        private void ResetToAllCurriculums()
        {
            Curriculums = new ObservableCollection<Curriculum>(_allCurriculums);
        }
    }
    #endregion
}

