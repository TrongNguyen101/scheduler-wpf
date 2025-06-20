using SchedulerWpfApp.Repository;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

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
            return await _unitOfWork.Repository<Curriculum>().GetAllAsync();
        }

        /// <summary>
        /// Adds a new Curriculum to the database
        /// </summary>
        /// <param name="Curriculum">The person entity to add</param>
        public async Task AddCurriculum(Curriculum curriculum)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Curriculum>().AddAsync(curriculum);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Deletes a subject from the database by their curriculumCode
        /// </summary>
        /// <param name="curriculumCode">The ID of the curriculum to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no curriculum with the specified ID exists, no action is taken</remarks>
        public async Task DeleteCurriculum(string curriculumCode)
        {
            var existingCurriculum = await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(curriculumCode);
            if (existingCurriculum != null)
            {
                await _unitOfWork.CurriculumRepository.DeleteCurriculum(curriculumCode);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves a specific curriculum by their ID
        /// </summary>
        /// <param name="CurriculumCode">The ID of the curriculum to retrieve</param>
        /// <returns>The curriculum with the specified ID, or null if not found</returns>
        public async Task<Curriculum?> GetByCurriculumCodeAsync(string CurriculumCode)
        {
            return await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(CurriculumCode);
        }

        /// <summary>
        /// Updates an existing curriculum in the database
        /// </summary>
        /// <param name="Curriculum">The curriculum specified ID exists, no action is taken</remarks>
        public async Task UpdateCurriculum(Curriculum curriculum)
        {
            var existingCurriculum = await _unitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(curriculum.CurriculumCode);
            /// <returns>A task representing the asynchronous operation</returns>
            /// <remarks>If no Curriculum with thnitOfWork.CurriculumRepository.GetByCurriculumCodeAsync(Curriculum.CurriculumId);
            if (existingCurriculum != null)
            {
                existingCurriculum.CurriculumCode = curriculum.CurriculumCode;
                existingCurriculum.IsActive = curriculum.IsActive;

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Curriculum>().UpdateAsync(existingCurriculum);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Imports a list of curriculums from an Excel file into the database
        /// </summary>
        /// <param name="listCurriculumFromExcel"></param>
        public async Task ImportCurriculumFromExcel(List<Curriculum> listCurriculumFromExcel)
        {
            foreach (var curriculum in listCurriculumFromExcel)
            {
                await _unitOfWork.Repository<Curriculum>().AddAsync(curriculum);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Reads curriculums from an Excel file and returns a list of Curriculum objects
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>List<Curriculum></returns>
        public List<Curriculum> ReadCurriculumsFromExcel(string filePath)
        {
            var curriculums = new List<Curriculum>();

            using ExcelEngine excelEngine = new();
            var app = excelEngine.Excel;
            app.DefaultVersion = ExcelVersion.Xlsx;

            var workbook = app.Workbooks.Open(filePath);
            var sheet = workbook.Worksheets[0];

            int rowCount = sheet.UsedRange.LastRow;
            int colCount = sheet.UsedRange.LastColumn;

            Dictionary<string, int> headerMap = new();
            for (int c = 1; c <= colCount; c++)
            {
                string header = sheet[1, c].Value?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(header))
                    headerMap[header] = c;
            }

            string[] requiredHeaders = { "CurriculumCode", "IsActive" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;

                bool isActive = true; // Default value for IsActive
                var curriculum = new Curriculum
                {
                    CurriculumCode = sheet[r, headerMap["CurriculumCode"]].Value,
                    IsActive = bool.TryParse(sheet[r, headerMap["IsActive"]].Value?.ToString(), out isActive)
                };

                curriculums.Add(curriculum);
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
