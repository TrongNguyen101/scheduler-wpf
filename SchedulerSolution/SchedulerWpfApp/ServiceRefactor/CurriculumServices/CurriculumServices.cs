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
        #endregion
    }
}
