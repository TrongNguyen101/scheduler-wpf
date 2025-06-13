using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ServiceRefactor.LecturerServices
{
    public class LecturerServices: ILecturerServices
    {
        #region Fields
        private IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the LecturerServices class
        /// </summary>
        /// <param name="unitOfWork">The database context used for data operations</param>
        public LecturerServices(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; // Injecting the unit of work to manage database operations
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new lecture to the database
        /// </summary>
        /// <param name="lecture">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddLecture(Lecturer lecture)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Lecturer>().AddAsync(lecture);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Imports a list of lecturers from an Excel file into the database
        /// </summary>
        /// <param name="listLectureFromExcel"></param>
        /// <returns></returns>
        public async Task ImportLectureFromExcel(List<Lecturer> listLectureFromExcel)
        {
            foreach (var lecture in listLectureFromExcel)
            {
                await _unitOfWork.LecturerRepository.AddAsync(lecture);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a subject from the database by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecture to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecture with the specified ID exists, no action is taken</remarks>
        public async Task DeleteLecture(string LecturerId)
        {
            var existingLecture = await _unitOfWork.LecturerRepository.GetByLectureCodeAsync(LecturerId);
            if (existingLecture != null)
            {
                await _unitOfWork.LecturerRepository.DeleteLecture(LecturerId);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves a specific lecture by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecture to retrieve</param>
        /// <returns>The lecture with the specified ID, or null if not found</returns>
        public async Task<Lecturer?> GetByLectureCodeAsync(string LecturerId)
        {
            return await _unitOfWork.LecturerRepository.GetByLectureCodeAsync(LecturerId);
        }

        /// <summary>
        /// Retrieves all lectures from the database
        /// </summary>
        /// <returns>A list of all lectures in the database</returns>
        public async Task<List<Lecturer>> GetAllLecturerAsync()
        {
            return await _unitOfWork.Repository<Lecturer>().GetAllAsync();
        }

        /// <summary>
        /// Updates an existing lecture in the database
        /// </summary>
        /// <param name="lecture">The lecture entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecture with the specified ID exists, no action is taken</remarks>
        public async Task UpdateLecture(Lecturer lecture)
        {
            var existingLecture = await _unitOfWork.LecturerRepository.GetByLectureCodeAsync(lecture.LecturerId);
            if (existingLecture != null)
            {
                existingLecture.LecturerName = lecture.LecturerName;
                existingLecture.Role = lecture.Role;
                existingLecture.Department = lecture.Department;

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Lecturer>().UpdateAsync(existingLecture);
                await _unitOfWork.CommitAsync();
            }
        }

        /// <summary>
        /// Reads lecturers from an Excel file and returns a list of Lecturer objects
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<Lecturer> ReadLecturersFromExcel(string filePath)
        {
            var lectures = new List<Lecturer>();

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

            string[] requiredHeaders = { "MaNV", "Fullname", "Bomon", "LoaiGV" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;
                var lecture = new Lecturer
                {
                    LecturerId = sheet[r, headerMap["MaNV"]].Value,
                    LecturerName = sheet[r, headerMap["Fullname"]].Value,
                    Role = sheet[r, headerMap["LoaiGV"]].Value,
                    Department = sheet[r, headerMap["Bomon"]].Value
                };

                lectures.Add(lecture);
            }
            return lectures;
        }

        /// <summary>
        /// Exports a list of lecturers to an Excel file
        /// </summary>
        /// <param name="lectures"></param>
        /// <param name="filePath"></param>
        public void ExportToExcel(List<Lecturer> lectures, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            // Header
            sheet[1, 1].Text = "MaNV";
            sheet[1, 2].Text = "Fullname";
            sheet[1, 3].Text = "Bomon";
            sheet[1, 4].Text = "LoaiGV";

            int row = 2;
            foreach (var lecture in lectures)
            {
                sheet[row, 1].Text = lecture.LecturerId ?? "";
                sheet[row, 2].Text = lecture.LecturerName ?? "";
                sheet[row, 3].Text = lecture.Department ?? "";
                sheet[row, 4].Text = lecture.Role ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
        #endregion
    }
}
