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
        public async Task ImportLectureSubjectFromExcel(List<LecturerSubject> listlecturesubjectFromExcel)
        {
            try
            {
                foreach (var lecturesubject in listlecturesubjectFromExcel)
                {
                    await _unitOfWork.LectureSubjectRepository.AddAsync(lecturesubject);
                }
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("lỗi", ex);
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
                throw;
            }
        }

        /// <summary>
        /// Updates an existing lecturer subject in the database asynchronously.
        /// </summary>
        public async Task UpdateAsync(LecturerSubject lecturerSubject)
        {
            var existingLecturerSubject = await _unitOfWork.LectureSubjectRepository.GetLectureSubjectByCodeAsync(lecturerSubject.Id);
            //if (existingLecturerSubject == null)
            //{
            //    throw new Exception("LecturerSubject not found");
            //}
            //var existinglecture = await _context.Lecturers.FindAsync(lecturerSubject.LecturerId);
            //if (existinglecture == null)
            //{
            //    throw new Exception("Lecturer not found");
            //}
            //existingLecturerSubject.LecturerId = lecturerSubject.LecturerId;
            //existingLecturerSubject.SubjectCode = lecturerSubject.SubjectCode;
            //existingLecturerSubject.LecturerName = existinglecture.LecturerName;
            //existingLecturerSubject.NumberOfClasses = lecturerSubject.NumberOfClasses;
            //await _context.SaveChangesAsync();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<LecturerSubject>().UpdateAsync(existingLecturerSubject);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
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
                throw;
            }
        }
        public List<LecturerSubject> ReadLectureSubjectFromExcel(string filePath)
        {
            var lecturesubjects = new List<LecturerSubject>();

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

            string[] requiredHeaders = { "LecturerId", "LecturerName", "SubjectCode", "SubjectName", "Major", "Term", "NumberOfClasses", "TotalSLots" };
            foreach (var h in requiredHeaders)
                if (!headerMap.ContainsKey(h))
                    throw new Exception($"Missing required column: {h}");

            for (int r = 2; r <= rowCount; r++)
            {
                var lecturesubject = new LecturerSubject
                {
                    LecturerId = sheet[r, headerMap["LecturerId"]].Value,
                    LecturerName = sheet[r, headerMap["LecturerName"]].Value,
                    SubjectCode = sheet[r, headerMap["SubjectCode"]].Value,
                    SubjectName = sheet[r, headerMap["SubjectName"]].Value,
                    Major = sheet[r, headerMap["Major"]].Value,
                    Term = sheet[r, headerMap["Term"]].Value,
                    TotalSlots = int.TryParse(sheet[r, headerMap["NumberOfClasses"]].Value, out int totalslots) ? totalslots : 0,
                    NumberOfClasses = int.TryParse(sheet[r, headerMap["NumberOfClasses"]].Value, out int numberOfClasses) ? numberOfClasses : 0
                };

                lecturesubjects.Add(lecturesubject);
            }
            return lecturesubjects;
        }
        #endregion
    }
}
