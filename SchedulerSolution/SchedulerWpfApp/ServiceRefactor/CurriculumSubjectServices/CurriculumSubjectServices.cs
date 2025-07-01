using SchedulerWpfApp.Repository;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;
using System.Windows;
using System.IO;
using System.Resources;
using SchedulerWpfApp.Helper;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices
{
    public class CurriculumSubjectServices : ICurriculumSubjectServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the CurriculumSubjectServices class
        /// </summary>
        /// <param name="unitOfWork">The database context used for data operations</param>
        public CurriculumSubjectServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new curriculumSubject to the database
        /// </summary>
        /// <param name="curriculumSubject">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddCurriculumSubject(CurriculumSubject curriculumSubject)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<CurriculumSubject>().AddAsync(curriculumSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("An error occurred while adding the curriculum subject.", ex);
            }
        }

        /// <summary>
        /// Deletes a curriculumSubject from the database by their ID
        /// </summary>
        /// <param name="id">The ID of the curriculumSubject to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no curriculumSubject with the specified ID exists, no action is taken</remarks>
        public async Task DeleteCurriculumSubject(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCurriculumSubject = await _unitOfWork.Repository<CurriculumSubject>().GetByIdAsync(id);
                if (existingCurriculumSubject != null)
                {
                    await _unitOfWork.CurriculumSubjectsRepository.DeleteAsync(id);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("An error occurred while deleting the curriculum subject.", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific curriculumSubject by their ID
        /// </summary>
        /// <param name="id">The ID of the curriculumSubject to retrieve</param>
        /// <returns>The curriculumSubject with the specified ID, or null if not found</returns>
        public async Task<CurriculumSubject?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<CurriculumSubject>().GetByIdAsync(id);
        }

        /// <summary>
        /// Updates an existing curriculumSubject in the database
        /// </summary>
        /// <param name="curriculumSubject">The curriculumSubject entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no curriculumSubject with the specified ID exists, no action is taken</remarks>
        public async Task UpdateCurriculumSubject(CurriculumSubject curriculumSubject)
        {
            var existingCurriculumSubject = await _unitOfWork.Repository<CurriculumSubject>().GetByIdAsync(curriculumSubject.Id);
            if (existingCurriculumSubject != null)
            {
                existingCurriculumSubject.CurriculumCode = curriculumSubject.CurriculumCode;
                existingCurriculumSubject.SubjectCode = curriculumSubject.SubjectCode;
                existingCurriculumSubject.SubjectNameEnglish = curriculumSubject.SubjectNameEnglish;
                existingCurriculumSubject.SubjectNameVietnamese = curriculumSubject.SubjectNameVietnamese;
                existingCurriculumSubject.TermNo = curriculumSubject.TermNo;
                existingCurriculumSubject.IsCombo = curriculumSubject.IsCombo;
                existingCurriculumSubject.Credit = curriculumSubject.Credit;
                existingCurriculumSubject.TotalSlots = curriculumSubject.TotalSlots;

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<CurriculumSubject>().UpdateAsync(existingCurriculumSubject);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Retrieves all curriculum subjects from the database asynchronously
        /// </summary>
        /// <returns>Task<List<CurriculumSubject>></returns>
        public async Task<List<CurriculumSubject>> GetAllCurriculumSubjectAsync()
        {
            try
            {
                var curriculums = await _unitOfWork.Repository<CurriculumSubject>().GetAllAsync();
                return curriculums;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving curriculum subjects.", ex);
            }
        }

        /// <summary>
        /// Imports a list of curriculum subjects from an Excel file into the database
        /// </summary>
        /// <param name="listCurriculumSubjectFromExcel"></param>
        public async Task ImportCurriculumSubjectFromExcel(List<CurriculumSubject> listCurriculumSubjectFromExcel)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var curriculumMissList = new List<string>();
                var subjectMissList = new List<string>();

                foreach (var curriculumSubject in listCurriculumSubjectFromExcel)
                {
                    var curriculum = await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(curriculumSubject.CurriculumCode);
                    if (curriculum == null)
                    {
                        curriculumMissList.Add(curriculumSubject.CurriculumCode);
                        continue;
                    }

                    var subject = await _unitOfWork.SubjectRepository.GetSubjectByCodeAsync(curriculumSubject.SubjectCode);
                    if (subject == null)
                    {
                        subjectMissList.Add(curriculumSubject.SubjectCode);
                        continue;
                    }

                    var existingCurriculumSubject = await _unitOfWork.CurriculumSubjectsRepository.CheckCurriculumSubjectCodeExistsAsync(curriculumSubject);
                    if (!existingCurriculumSubject)
                    {
                        await _unitOfWork.CurriculumSubjectsRepository.AddAsync(curriculumSubject);
                    }
                }
                await _unitOfWork.CommitAsync();

                if (curriculumMissList.Count > 0 && subjectMissList.Count > 0)
                {
                    MessageBox.Show("Không tìm thấy khung chương trình với mã: " + string.Join(", ", curriculumMissList) + "\n" + "Không tìm thấy môn học với mã:" + string.Join(", ", subjectMissList), "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (curriculumMissList.Count > 0)
                {
                    MessageBox.Show("Không tìm thấy khung chương trình với mã: " + string.Join(", ", curriculumMissList), "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /// Reads curriculum subjects from an Excel file and returns a list of curriculum subject objects
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>List<CurriculumSubject></returns>
        public List<CurriculumSubject> ReadCurriculumSubjectsFromExcel(string filePath)
        {
            var curriculumSubjects = new List<CurriculumSubject>();
            var curriculumSubjectLineMap = new Dictionary<string, List<int>>();
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

                    Utility.IsOnlyHeader(worksheet);
                    Utility.IsEmptyExcelRow(worksheet, rowCount, colCount);
                    Utility.IsColumnDuplicatedCSExcel(worksheet);

                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "CurriculumCode", "SubjectCode", "SubjectName", "SubjectV", "TermNo", "IsCombo", "Credits", "TotalSLots" };
                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Missing required column: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(worksheet[r, headerMap[h]].Value));
                        if (isEmptyRow)
                            continue;

                        var curriculumCode = worksheet[r, headerMap["CurriculumCode"]].Value?.ToString();
                        var subjectCode = worksheet[r, headerMap["SubjectCode"]].Value?.ToString();
                        var subjectNameEnglish = worksheet[r, headerMap["SubjectName"]].Value?.ToString();
                        var subjectNameVietnamese = worksheet[r, headerMap["SubjectV"]].Value?.ToString();
                        var teachingMode = worksheet[r, headerMap["TeachingMode"]].Value?.ToString();

                        // TermNo
                        int termNo = 0;
                        int.TryParse(worksheet[r, headerMap["TermNo"]].Value?.ToString(), out termNo);

                        // IsCombo
                        bool isCombo = false;
                        bool.TryParse(worksheet[r, headerMap["IsCombo"]].Value?.ToString(), out isCombo);

                        // Credit
                        int credit = 0;
                        int.TryParse(worksheet[r, headerMap["Credits"]].Value?.ToString(), out credit);

                        // TotalSlots
                        int? totalSlots = null;

                        if (int.TryParse(worksheet[r, headerMap["TotalSLots"]].Value?.ToString(), out int slots))
                        {
                            totalSlots = slots;
                        }

                        string key = $"{curriculumCode}-{subjectCode}-{termNo}";
                        var curriculumSubject = new CurriculumSubject
                        {
                            CurriculumCode = curriculumCode,
                            SubjectCode = subjectCode,
                            SubjectNameEnglish = subjectNameEnglish,
                            SubjectNameVietnamese = subjectNameVietnamese,
                            TermNo = termNo,
                            IsCombo = isCombo,
                            Credit = credit,
                            TotalSlots = totalSlots,
                            TeachingMode = teachingMode
                        };

                        curriculumSubjects.Add(curriculumSubject);

                        if (!curriculumSubjectLineMap.ContainsKey(key))
                        {
                            curriculumSubjectLineMap[key] = new List<int>();
                        }
                        curriculumSubjectLineMap[key].Add(r);
                    }
                }
            }
            return curriculumSubjects;
        }

        /// <summary>
        /// Exports a list of curriculumSubjects to an Excel file
        /// </summary>
        /// <param name="curriculumSubjects"></param>
        /// <param name="filePath"></param>
        public void ExportToExcel(List<CurriculumSubject> curriculumSubjects, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "CurriculumCode";
            sheet[1, 2].Text = "SubjectCode";
            sheet[1, 3].Text = "SubjectName";
            sheet[1, 4].Text = "SubjectV";
            sheet[1, 5].Text = "TermNo";
            sheet[1, 6].Text = "IsCombo";
            sheet[1, 7].Text = "Credits";
            sheet[1, 8].Text = "TotalSLots";

            int row = 2;
            foreach (var curriculumSubject in curriculumSubjects)
            {
                sheet[row, 1].Text = curriculumSubject.CurriculumCode ?? "";
                sheet[row, 2].Text = curriculumSubject.SubjectCode ?? "";
                sheet[row, 3].Text = curriculumSubject.SubjectNameEnglish ?? "";
                sheet[row, 4].Text = curriculumSubject.SubjectNameVietnamese ?? "";
                sheet[row, 5].Text = curriculumSubject.TermNo.ToString() ?? "";
                sheet[row, 6].Text = curriculumSubject.IsCombo.ToString() ?? "";
                sheet[row, 7].Text = curriculumSubject.Credit.ToString() ?? "";
                sheet[row, 8].Text = curriculumSubject.TotalSlots.ToString() ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
        #endregion
    }
}
