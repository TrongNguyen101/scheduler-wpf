using System.IO;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ServiceRefactor.SubjectServices
{
    public class SubjectServices : ISubjectServices
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Contructor
        /// <summary>
        /// Initializes a new instance of the SubjectServices class
        /// </summary>
        /// <param name="unitOfWork"></param>
        public SubjectServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new subject to the database
        /// </summary>
        /// <param name="subject">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddSubject(Subject subject)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Subject>().AddAsync(subject);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Deletes a subject from the database by their ID
        /// </summary>
        /// <param name="id">The ID of the subject to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no subject with the specified ID exists, no action is taken</remarks>
        public async Task DeleteSubject(string subjectCode)
        {
            var existingSubject = await GetBySubjectCodeAsync(subjectCode);
            if (existingSubject != null)
            {
                await _unitOfWork.SubjectRepository.DeleteAsync(subjectCode);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves all subjects from the database
        /// </summary>
        /// <returns>A list of all subjects in the database</returns>
        public async Task<List<Subject>> GetAllAsync()
        {
            return await _unitOfWork.Repository<Subject>().GetAllAsync();
        }

        /// <summary>
        /// Retrieves a specific subject by their ID
        /// </summary>
        /// <param name="id">The ID of the subject to retrieve</param>
        /// <returns>The subject with the specified ID, or null if not found</returns>
        public async Task<Subject?> GetBySubjectCodeAsync(string subjectCode)
        {
            return await _unitOfWork.SubjectRepository.GetSubjectByCodeAsync(subjectCode);
        }

        /// <summary>
        /// Updates an existing subject in the database
        /// </summary>
        /// <param name="subject">The subject entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no subject with the specified ID exists, no action is taken</remarks>
        public async Task UpdateSubject(Subject subject)
        {
            var existingSubject = await GetBySubjectCodeAsync(subject.SubjectCode);
            if (existingSubject != null)
            {
                existingSubject.SubjectNameEnglish = subject.SubjectNameEnglish;
                existingSubject.SubjectNameVietnamese = subject.SubjectNameVietnamese;
                existingSubject.TotalCredits = subject.TotalCredits;
                existingSubject.TotalTime = subject.TotalTime;
                //_mapper.Map(subject, existingSubject);

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Subject>().UpdateAsync(existingSubject);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Imports a list of subjects from an Excel file into the database
        /// </summary>
        /// <param name="listSubjectFromExcel"></param>
        /// <returns></returns>
        public async Task ImportSubjectFromExcel(List<Subject> listSubjectFromExcel)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var subject in listSubjectFromExcel)
                {
                    var existingSubject = await _unitOfWork.SubjectRepository.CheckSubjectCodeExistsAsync(subject.SubjectCode);
                    if (!existingSubject)
                    {
                        await _unitOfWork.SubjectRepository.AddAsync(subject);
                    }
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Có lỗi trong quá trình import dữ liệu", ex);
            }
        }

        /// <summary>
        /// Exports a list of subjects to an Excel file
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="filePath"></param>
        public void ExportToExcel(List<Subject> subjects, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "SubjectCode";
            sheet[1, 2].Text = "SubjectNameEnglish";
            sheet[1, 3].Text = "SubjectNameVietnamese";
            sheet[1, 4].Text = "TotalTime";
            sheet[1, 5].Text = "TotalCredits";

            int row = 2;
            foreach (var subject in subjects)
            {
                sheet[row, 1].Text = subject.SubjectCode ?? "";
                sheet[row, 2].Text = subject.SubjectNameEnglish ?? "";
                sheet[row, 3].Text = subject.SubjectNameVietnamese ?? "";
                sheet[row, 4].Text = subject.TotalTime.ToString() ?? "";
                sheet[row, 5].Text = subject.TotalCredits.ToString() ?? "";
                row++;
            }

            workbook.SaveAs(filePath);
        }

        /// <summary>
        /// Reads subjects from an Excel file and returns a list of Subject objects
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<Subject> ReadSubjectsFromExcel(string filePath)
        {
            var subjects = new List<Subject>();
            var subjectLineMap = new Dictionary<string, List<int>>();

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
                    var listColCheck = new List<int> { 1 };
                    Utility.IsDuplicatedExcelRow(worksheet, rowCount, listColCheck);
                    Dictionary<string, int> headerMap = new();
                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "SubjectCode", "SubjectNameEnglish", "SubjectNameVietnamese", "TotalTime", "TotalCredits" };
                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Missing required column: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(worksheet[r, headerMap[h]].Value));

                        if (isEmptyRow)
                            continue;

                        var subject = new Subject
                        {
                            SubjectCode = worksheet[r, headerMap["SubjectCode"]].Value,
                            SubjectNameEnglish = worksheet[r, headerMap["SubjectNameEnglish"]].Value,
                            SubjectNameVietnamese = worksheet[r, headerMap["SubjectNameVietnamese"]].Value,
                            TotalTime = int.TryParse(worksheet[r, headerMap["TotalTime"]].Value, out int totalSessions) ? totalSessions : 0,
                            TotalCredits = int.TryParse(worksheet[r, headerMap["TotalCredits"]].Value, out int SlotsPerWeek) ? SlotsPerWeek : 0
                        };

                        subjects.Add(subject);
                    }
                }
            }
            return subjects;
        }
        #endregion
    }
}
