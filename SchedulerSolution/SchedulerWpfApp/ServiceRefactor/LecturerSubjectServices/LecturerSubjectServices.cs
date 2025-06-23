using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using Syncfusion.XlsIO;

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
        public async Task ImportLecturerSubjectFromExcel(List<LecturerSubject> listLectuerSubjectFromExcel)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                foreach (var lecturerSubject in listLectuerSubjectFromExcel)
                {
                    var existingLecturerSubject = await _unitOfWork.LecturerSubjectRepository.CheckLecturerSubjectExits(lecturerSubject);
                    if (!existingLecturerSubject)
                    {
                        await _unitOfWork.Repository<LecturerSubject>().AddAsync(lecturerSubject);
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
        /// Retrieves a lecturer subject by its ID asynchronously.
        /// </summary>
        public async Task<LecturerSubject> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<LecturerSubject>().GetByIdAsync(id);
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
            var lecturerSubjecLineMap = new Dictionary<string, List<int>>();
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

            string[] requiredHeaders = { "MAGV", "GIANGVIEN", "MAMH", "TENMH", "NGANH", "KY", "SLL", "TONGSLOT" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                bool isEmptyRow = requiredHeaders.All(h => string.IsNullOrWhiteSpace(sheet[r, headerMap[h]].Value));
                if (isEmptyRow)
                    continue;
                string lecturerId = sheet[r, headerMap["MAGV"]].Value;
                string subjectCode = sheet[r, headerMap["MAMH"]].Value;
                string term = sheet[r, headerMap["KY"]].Value;
                string key = $"{lecturerId}-{subjectCode}-{term}";
                var lecturerSubject = new LecturerSubject
                {
                    LecturerId = sheet[r, headerMap["MAGV"]].Value,
                    LecturerName = sheet[r, headerMap["GIANGVIEN"]].Value,
                    SubjectCode = sheet[r, headerMap["MAMH"]].Value,
                    SubjectName = sheet[r, headerMap["TENMH"]].Value,
                    Major = sheet[r, headerMap["NGANH"]].Value,
                    Term = sheet[r, headerMap["KY"]].Value,
                    NumberOfClasses = int.TryParse(sheet[r, headerMap["SLL"]].Value, out int totalslots) ? totalslots : 0,
                    TotalSlots = int.TryParse(sheet[r, headerMap["TONGSLOT"]].Value, out int numberOfClasses) ? numberOfClasses : 0
                };
                lecturerSubjects.Add(lecturerSubject);
                if (!lecturerSubjecLineMap.ContainsKey(key)){
                    lecturerSubjecLineMap[key] = new List<int>();
                }
                lecturerSubjecLineMap[key].Add(r);    
            }
            var duplicateLecturerSubjects = lecturerSubjecLineMap
                .Where(ls => ls.Value.Count > 1)
                .ToDictionary(ls => ls.Key, ls => ls.Value);
            if (duplicateLecturerSubjects.Count > 0)
            {
                var errorMessage = duplicateLecturerSubjects
                   .Select(dlc => $"LecturerSubject bị trùng tại các dòng: {string.Join(", ", dlc.Value)}");
                throw new Exception("Phát hiện dữ liệu trùng trong file Excel:\n " + string.Join("\n", errorMessage));
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
                sheet[row, 6].Text = lecturerSubject.Term ?? "";
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
        public Task<bool> CheckLecturerSubjectExits(LecturerSubject lecturerSubject)
        {
            return _unitOfWork.LecturerSubjectRepository.CheckLecturerSubjectExits(lecturerSubject);
        }
        #endregion
    }
}
