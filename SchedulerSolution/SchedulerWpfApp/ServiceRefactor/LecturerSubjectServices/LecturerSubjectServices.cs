using System.IO;
using System.Windows;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices
{
    public class LecturerSubjectServices : ILecturerSubjectServices
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="LecturerSubjectServices"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work instance for data access.</param>
        public LecturerSubjectServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Method
        /// <summary>
        /// Retrieves all LecturerSubject entities asynchronously from the repository.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of LecturerSubject entities.</returns>
        public async Task<List<LecturerSubject>> GetAllAsync()
        {
            return await _unitOfWork.Repository<LecturerSubject>().GetAllAsync();
        }

        /// <summary>
        /// Imports a list of lecturer subjects from an Excel file into the database.
        /// </summary>
        public async Task ImportLecturerSubjectFromExcel(List<LecturerSubject> listLecturerSubjectFromExcel, IProgress<int> progress)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var index = 0;
                var lecturerMissList = new List<string>();
                var subjectMissList = new List<string>();

                foreach (var lecturerSubject in listLecturerSubjectFromExcel)
                {
                    var lecturer = await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(lecturerSubject.LecturerId);
                    if (lecturer == null)
                    {
                        lecturerMissList.Add(lecturerSubject.LecturerId);
                        continue;
                    }

                    var subject = await _unitOfWork.SubjectRepository.GetSubjectByCodeAsync(lecturerSubject.SubjectCode);
                    if (subject == null)
                    {
                        subjectMissList.Add(lecturerSubject.SubjectCode);
                        continue;
                    }

                    var existingLecturerSubject = await _unitOfWork.LecturerSubjectRepository.CheckLecturerSubjectExits(lecturerSubject);
                    if (!existingLecturerSubject)
                    {
                        await _unitOfWork.Repository<LecturerSubject>().AddAsync(lecturerSubject);
                    }

                    await Task.Delay(10);

                    index++;
                    var percentCompleted = (int)((double)index / listLecturerSubjectFromExcel.Count * 100);
                    progress?.Report(percentCompleted);
                }

                await _unitOfWork.CommitAsync();

                if (lecturerMissList.Count > 0 && subjectMissList.Count > 0)
                {
                    MessageBox.Show("Không tìm thấy giảng viên với mã: " + string.Join(", ", lecturerMissList) + "\n" + "Không tìm thấy giảng viên với mã:" + string.Join(", ", subjectMissList), "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (lecturerMissList.Count > 0)
                {
                    MessageBox.Show("Không tìm thấy giảng viên với mã: " + string.Join(", ", lecturerMissList), "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (subjectMissList.Count > 0)
                {
                    MessageBox.Show("Không tìm thấy môn học với mã: " + string.Join(", ", subjectMissList), "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Có lỗi trong quá trình import dữ liệu", ex);
            }
        }

        /// <summary>
        /// Retrieves a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task<LecturerSubject> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<LecturerSubject>().GetByIdAsync(id);
        }

        /// <summary>
        /// Retrieves a lecturer subject by its subject code asynchronously.
        /// </summary>
        /// <param name="subjectCode"></param>
        /// <returns></returns>
        public async Task<List<LecturerSubject>> GetBySubjectCodeAsync(string subjectCode)
        {
            return await _unitOfWork.LecturerSubjectRepository.GetLecturerSubjectBySubjectCodeAsync(subjectCode);
        }

        /// <summary>
        /// Adds a new lecturer subject to the database asynchronously.
        /// </summary>
        public async Task AddAsync(LecturerSubject lecturerSubject)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().AddAsync(lecturerSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Thêm phân công không thành công", ex);
            }
        }

        /// <summary>
        /// Updates an existing lecturer subject in the database asynchronously.
        /// </summary>
        public async Task UpdateAsync(LecturerSubject lecturerSubject)
        {
            await _unitOfWork.BeginTransactionAsync();
            var existingLecturerSubject = await _unitOfWork.LecturerSubjectRepository.GetLecturerSubjectByIdAsync(lecturerSubject.Id);
            if (existingLecturerSubject == null)
            {
                throw new Exception("Không tìm thấy phân công cần cập nhật");
            }
            try
            {
                existingLecturerSubject.LecturerId = lecturerSubject.LecturerId;
                existingLecturerSubject.SubjectCode = lecturerSubject.SubjectCode;
                existingLecturerSubject.LecturerName = lecturerSubject.LecturerName;
                existingLecturerSubject.NumberOfClasses = lecturerSubject.NumberOfClasses;
                existingLecturerSubject.SubjectName = lecturerSubject.SubjectName;
                existingLecturerSubject.Major = lecturerSubject.Major;
                existingLecturerSubject.Term = lecturerSubject.Term;
                existingLecturerSubject.TotalSlots = lecturerSubject.TotalSlots;
                await _unitOfWork.Repository<LecturerSubject>().UpdateAsync(existingLecturerSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Cập nhật phân công không thành công", ex);
            }
        }

        /// <summary>
        /// Deletes a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().DeleteAsync(id);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Xóa phân công không thành công", ex);
            }
        }
        /// <summary>
        /// Reads lecturer subject data from an Excel file and maps it to a list of LecturerSubject objects.
        /// </summary>
        /// <param name="filePath">The path to the Excel file.</param>
        /// <returns>A list of LecturerSubject objects read from the Excel file.</returns>
        public List<LecturerSubject> ReadLecturerSubjectFromExcel(string filePath)
        {
            var lecturerSubjects = new List<LecturerSubject>();
            Dictionary<string, int> headerMap = new();

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = application.Workbooks.Open(fileStream);
                    IWorksheet worksheet = workbook.Worksheets[0];

                    int rowCount = worksheet.UsedRange.LastRow;
                    int colCount = worksheet.UsedRange.LastColumn;

                    Utility.IsEmptyExcelRow(worksheet, rowCount, colCount);

                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "MAGV", "GIANGVIEN", "MAMH", "TENMH", "NGANH", "KY", "SLL", "TONGSLOT" };

                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Missing required column: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(worksheet[r, headerMap[h]].Value));

                        if (isEmptyRow)
                            continue;

                        string lecturerId = worksheet[r, headerMap["MAGV"]].Value;
                        string subjectCode = worksheet[r, headerMap["MAMH"]].Value;
                        string term = worksheet[r, headerMap["KY"]].Value;
                        string key = $"{lecturerId}-{subjectCode}-{term}";
                        var lecturerSubject = new LecturerSubject
                        {
                            LecturerId = worksheet[r, headerMap["MAGV"]].Value,
                            LecturerName = worksheet[r, headerMap["GIANGVIEN"]].Value,
                            SubjectCode = worksheet[r, headerMap["MAMH"]].Value,
                            SubjectName = worksheet[r, headerMap["TENMH"]].Value,
                            Major = worksheet[r, headerMap["NGANH"]].Value,
                            Term = int.TryParse(worksheet[r, headerMap["KY"]].Value, out int termValue) ? termValue : 0,
                            NumberOfClasses = int.TryParse(worksheet[r, headerMap["SLL"]].Value, out int totalslots) ? totalslots : 0,
                            TotalSlots = int.TryParse(worksheet[r, headerMap["TONGSLOT"]].Value, out int numberOfClasses) ? numberOfClasses : 0
                        };
                        lecturerSubjects.Add(lecturerSubject);
                    }
                }
            }
            return lecturerSubjects;
        }

        /// <summary>
        /// Exports a list of LecturerSubject objects to an Excel file.
        /// </summary>
        /// <param name="lecturersubjects">The list of LecturerSubject objects to export.</param>
        /// <param name="filePath">The path where the Excel file will be saved.</param>
        public void ExportToLecturerSubjectExcel(List<LecturerSubject> lecturerSubjects, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];
            // Header
            sheet[1, 1].Text = "MAGV";
            sheet[1, 2].Text = "GIANGVIEN";
            sheet[1, 3].Text = "MAMH";
            sheet[1, 4].Text = "TENMH";
            sheet[1, 5].Text = "NGANH";
            sheet[1, 6].Text = "KY";
            sheet[1, 7].Text = "SLL";
            sheet[1, 8].Text = "TONGSLOT";
            int row = 2;
            foreach (var lecturerSubject in lecturerSubjects)
            {
                sheet[row, 1].Text = lecturerSubject.LecturerId ?? "";
                sheet[row, 2].Text = lecturerSubject.LecturerName ?? "";
                sheet[row, 3].Text = lecturerSubject.SubjectCode ?? "";
                sheet[row, 4].Text = lecturerSubject.SubjectName ?? "";
                sheet[row, 5].Text = lecturerSubject.Major ?? "";
                sheet[row, 6].Number = lecturerSubject.Term ?? 0;
                sheet[row, 7].Number = lecturerSubject.NumberOfClasses ?? 0;
                sheet[row, 8].Number = lecturerSubject.TotalSlots ?? 0;
                row++;
            }
            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Checks if a LecturerSubject entity already exists in the repository.
        /// </summary>
        /// <param name="lecturerSubject">The LecturerSubject entity to check for existence.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the entity exists; otherwise, false.</returns>
        public Task<LecturerSubject> CheckLecturerSubjectExits(LecturerSubject lecturerSubject)
        {
            return _unitOfWork.LecturerSubjectRepository.GetLecturerSubjectAsync(lecturerSubject);
        }

        private async Task<List<int>> IsNullValueAsync(string filePath)
        {
            List<int> nullRows = new List<int>();
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IWorkbook workbook = application.Workbooks.Open(fileStream);
                    IWorksheet worksheet = workbook.Worksheets[0];

                    int rowCount = worksheet.UsedRange.LastRow;
                    int colCount = worksheet.UsedRange.LastColumn;
                    for (int row = 2; row <= rowCount; row++)
                    {
                        bool hasNull = false;
                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet[row, col].Value;
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                hasNull = true;
                                break;
                            }
                        }

                        if (hasNull)
                        {
                            nullRows.Add(row);
                        }
                    }
                }
            }
            return nullRows;
        }
        #endregion
    }
}
