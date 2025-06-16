using SchedulerWpfApp.Repository;
using SchedulerWpfApp.Model;
using Syncfusion.XlsIO;

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
        /// Retrieves all curriculum subjects from the database asynchronously
        /// </summary>
        /// <returns>Task<List<CurriculumSubject>></returns>
        public async Task<List<CurriculumSubject>> GetAllCurriculumSubjectAsync()
        {
            var curriList = await _unitOfWork.Repository<CurriculumSubject>().GetAllAsync();
            return curriList;
        }

        /// <summary>
        /// Imports a list of curriculum subjects from an Excel file into the database
        /// </summary>
        /// <param name="listCurriculumSubjectFromExcel"></param>
        public async Task ImportCurriculumSubjectFromExcel(List<CurriculumSubject> listCurriculumSubjectFromExcel)
        {
            try
            {
                foreach (var curriculumSubject in listCurriculumSubjectFromExcel)
                {
                    await _unitOfWork.Repository<CurriculumSubject>().AddAsync(curriculumSubject);
                }
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
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

            string[] requiredHeaders = { "CurriculumCode", "SubjectCode", "SubjectName", "SubjectV", "TermNo", "IsCombo", "Credits", "TotalSLots" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;

                var curriculumCode = sheet[r, headerMap["CurriculumCode"]].Value?.ToString();
                var subjectCode = sheet[r, headerMap["SubjectCode"]].Value?.ToString();
                var subjectNameEnglish = sheet[r, headerMap["SubjectName"]].Value?.ToString();
                var subjectNameVietnamese = sheet[r, headerMap["SubjectV"]].Value?.ToString();

                // TermNo
                int termNo = 0;
                int.TryParse(sheet[r, headerMap["TermNo"]].Value?.ToString(), out termNo);

                // IsCombo
                bool isCombo = false;
                bool.TryParse(sheet[r, headerMap["IsCombo"]].Value?.ToString(), out isCombo);

                // Credit
                int credit = 0;
                int.TryParse(sheet[r, headerMap["Credits"]].Value?.ToString(), out credit);

                // TotalSlots
                int? totalSlots = null;
                if (int.TryParse(sheet[r, headerMap["TotalSLots"]].Value?.ToString(), out int slots))
                {
                    totalSlots = slots;
                }

                var curriculumSubject = new CurriculumSubject
                {
                    CurriculumCode = curriculumCode,
                    SubjectCode = subjectCode,
                    SubjectNameEnglish = subjectNameEnglish,
                    SubjectNameVietnamese = subjectNameVietnamese,
                    TermNo = termNo,
                    IsCombo = isCombo,
                    Credit = credit,
                    TotalSlots = totalSlots
                };

                curriculumSubjects.Add(curriculumSubject);
            }
            return curriculumSubjects;
        }
        #endregion
    }
}
