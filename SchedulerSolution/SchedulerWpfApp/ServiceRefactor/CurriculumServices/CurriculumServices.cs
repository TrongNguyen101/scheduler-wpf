using SchedulerWpfApp.Repository;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;
using SchedulerWpfApp.Helper;
using System.IO;

namespace SchedulerWpfApp.ServiceRefactor.CurriculumServices
{
    public class CurriculumServices : ICurriculumServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the CurriculumServices class
        /// </summary>
        /// <param name="unitOfWork">The database context used for data operations</param>
        public CurriculumServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
        }
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves all curriculums from the database asynchronously
        /// </summary>
        /// <returns>Task<List<Curriculum>></returns>
        public async Task<List<Curriculum>> GetAllCurriculumAsync()
        {
            try
            {
                return await _unitOfWork.Repository<Curriculum>().GetAllAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving curriculums", ex);
            }
        }

        /// <summary>
        /// Adds a new Curriculum to the database
        /// </summary>
        /// <param name="Curriculum">The person entity to add</param>
        public async Task AddCurriculum(Curriculum curriculum)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<Curriculum>().AddAsync(curriculum);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("An error occurred while adding the curriculum", ex);
            }
        }

        /// <summary>
        /// Deletes a subject from the database by their curriculumCode
        /// </summary>
        /// <param name="curriculumCode">The ID of the curriculum to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no curriculum with the specified ID exists, no action is taken</remarks>
        public async Task DeleteCurriculum(string curriculumCode)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCurriculum = await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(curriculumCode);
                if (existingCurriculum != null)
                {
                    await _unitOfWork.CurriculumRepository.DeleteCurriculum(curriculumCode);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("An error occurred while deleting the curriculum", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific curriculum by their ID
        /// </summary>
        /// <param name="CurriculumCode">The ID of the curriculum to retrieve</param>
        /// <returns>The curriculum with the specified ID, or null if not found</returns>
        public async Task<Curriculum?> GetByCurriculumCodeAsync(string CurriculumCode)
        {
            try
            {
                return await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(CurriculumCode);
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving the curriculum", ex);
            }
        }

        /// <summary>
        /// Updates an existing curriculum in the database
        /// </summary>
        /// <param name="Curriculum">The curriculum specified ID exists, no action is taken</remarks>
        public async Task UpdateCurriculum(Curriculum curriculum)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCurriculum = await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(curriculum.CurriculumCode);
                if (existingCurriculum != null)
                {
                    existingCurriculum.CurriculumCode = curriculum.CurriculumCode;
                    existingCurriculum.IsActive = curriculum.IsActive;

                    await _unitOfWork.Repository<Curriculum>().UpdateAsync(existingCurriculum);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("An error occurred while updating the curriculum", ex);
            }
        }

        /// <summary>
        /// Imports a list of curriculums from an Excel file into the database
        /// </summary>
        /// <param name="listCurriculumFromExcel"></param>
        public async Task ImportCurriculumFromExcel(List<Curriculum> listCurriculumFromExcel, IProgress<int> progress)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var index = 0;

                foreach (var curriculum in listCurriculumFromExcel)
                {
                    var existingCurriculum = await _unitOfWork.CurriculumRepository.CheckCurriculumCodeExistsAsync(curriculum.CurriculumCode);
                    if (!existingCurriculum)
                    {
                        await _unitOfWork.CurriculumRepository.AddAsync(curriculum);
                    }

                    await Task.Delay(10);

                    index++;
                    var percentCompleted = (int)((double)index / listCurriculumFromExcel.Count * 100);
                    progress?.Report(percentCompleted);
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
        /// Reads curriculums from an Excel file and returns a list of Curriculum objects
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>List<Curriculum></returns>
        public List<Curriculum> ReadCurriculumsFromExcel(string filePath)
        {
            var curriculums = new List<Curriculum>();
            Dictionary<string, int> headerMap = new Dictionary<string, int>();

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
                    Utility.IsColumnDuplicated(worksheet, "CurriculumCode");

                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";

                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "CurriculumCode", "IsActive" };
                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Thiếu cột bắt buộc: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(worksheet[r, headerMap[h]].Value));

                        if (isEmptyRow)
                            continue;

                        bool isActive = true; // Default value for IsActive
                        string curriculumCode = worksheet[r, headerMap["CurriculumCode"]].Value;
                        var curriculum = new Curriculum
                        {
                            CurriculumCode = worksheet[r, headerMap["CurriculumCode"]].Value,
                            IsActive = bool.TryParse(worksheet[r, headerMap["IsActive"]].Value?.ToString(), out isActive)
                        };
                        curriculums.Add(curriculum);
                    }

                    workbook.Close();
                }
            }
            return curriculums;
        }

        /// <summary>
        /// Exports a list of curriculums to an Excel file
        /// </summary>
        /// <param name="curriculums"></param>
        /// <param name="filePath"></param>
        public void ExportToExcel(List<Curriculum> curriculums, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "CurriculumCode";
            sheet[1, 2].Text = "IsActive";

            int row = 2;
            foreach (var curriculumn in curriculums)
            {
                sheet[row, 1].Text = curriculumn.CurriculumCode ?? "";
                sheet[row, 2].Text = curriculumn.IsActive.ToString() ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
        #endregion
    }
}
