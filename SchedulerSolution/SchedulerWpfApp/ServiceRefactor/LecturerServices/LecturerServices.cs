using System.IO;
using SchedulerWpfApp.Helper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.ServiceRefactor.LecturerServices
{
    public class LecturerServices : ILecturerServices
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
        /// Adds a new lecturer to the database
        /// </summary>
        /// <param name="lecturer">The person entity to add</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddLecturer(Lecturer lecturer)
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Repository<Lecturer>().AddAsync(lecturer);
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// Imports a list of lecturers from an Excel file into the database
        /// </summary>
        /// <param name="listLectureFromExcel"></param>
        /// <returns></returns>
        public async Task ImportLecturerFromExcel(List<Lecturer> listLecturerFromExcel, IProgress<int> progress)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var index = 0;

                foreach (var lecturer in listLecturerFromExcel)
                {
                    var existingLecturer = await _unitOfWork.LecturerRepository.CheckLecturerExistsAsync(lecturer.LecturerId);
                    if (!existingLecturer)
                    {
                        await _unitOfWork.LecturerRepository.AddAsync(lecturer);
                    }

                    await Task.Delay(10);

                    index++;
                    var percentCompleted = (int)((double)index / listLecturerFromExcel.Count * 100);
                    progress.Report(percentCompleted);
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
        /// Deletes a subject from the database by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecturer to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecturer with the specified ID exists, no action is taken</remarks>
        public async Task DeleteLecturer(string LecturerId)
        {
            var existingLecturer = await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(LecturerId);
            if (existingLecturer != null)
            {
                await _unitOfWork.LecturerRepository.DeleteLecturer(LecturerId);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves a specific lecturer by their ID
        /// </summary>
        /// <param name="LecturerId">The ID of the lecturer to retrieve</param>
        /// <returns>The lecturer with the specified ID, or null if not found</returns>
        public async Task<Lecturer?> GetByLecturerCodeAsync(string LecturerId)
        {
            return await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(LecturerId);
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
        /// Updates an existing lecturer in the database
        /// </summary>
        /// <param name="lecturer">The lecturer entity with updated values</param>
        /// <returns>A task representing the asynchronous operation</returns>
        /// <remarks>If no lecturer with the specified ID exists, no action is taken</remarks>
        public async Task UpdateLecturer(Lecturer lecturer)
        {
            var existingLecturer = await _unitOfWork.LecturerRepository.GetByLecturerCodeAsync(lecturer.LecturerId);
            if (existingLecturer != null)
            {
                existingLecturer.LecturerName = lecturer.LecturerName;
                existingLecturer.Role = lecturer.Role;
                existingLecturer.Department = lecturer.Department;

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<Lecturer>().UpdateAsync(existingLecturer);
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
                    Utility.IsColumnDuplicated(worksheet, "MaNV");

                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "MaNV", "Fullname", "Bomon", "LoaiGV", "accGV" };

                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Thiếu cột bắt buộc: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(worksheet[r, headerMap[h]].Value));

                        if (isEmptyRow)
                            continue;

                        var lecturer = new Lecturer
                        {
                            LecturerId = worksheet[r, headerMap["MaNV"]].Value,
                            LecturerName = worksheet[r, headerMap["Fullname"]].Value,
                            Role = worksheet[r, headerMap["LoaiGV"]].Value,
                            Department = worksheet[r, headerMap["Bomon"]].Value,
                            LecturerAccount = worksheet[r, headerMap["accGV"]].Value
                        };

                        lectures.Add(lecturer);
                    }
                }
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
            sheet[1, 5].Text = "accGV";

            int row = 2;
            foreach (var lecturer in lectures)
            {
                sheet[row, 1].Text = lecturer.LecturerId ?? "";
                sheet[row, 2].Text = lecturer.LecturerName ?? "";
                sheet[row, 3].Text = lecturer.Department ?? "";
                sheet[row, 4].Text = lecturer.Role ?? "";
                sheet[row, 5].Text = lecturer.LecturerAccount ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }
        #endregion
    }
}
