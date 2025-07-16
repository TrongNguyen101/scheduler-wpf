using SchedulerWpfApp.Repository;
using SchedulerWpfApp.Model;
using AutoMapper;
using Syncfusion.XlsIO;
using System.IO;
using SchedulerWpfApp.Helper;
namespace SchedulerWpfApp.ServiceRefactor.GroupNameService
{
    public class GroupNameService : IGroupNameService
    {
        #region Fields
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Contracstor
        public GroupNameService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create a new instance of GroupNameService with the provided DataContext.
        /// This method adds a new group name (class) to the database.
        /// It takes a GroupName object as input and saves it asynchronously.
        /// </summary>
        /// <param name="groupName"></param>
        public async Task AddGroupName(GroupClass groupName)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Repository<GroupClass>().AddAsync(groupName);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi lấy dữ liệu lớp học", ex);
            }
        }

        /// <summary>
        /// Delete a group name (class) by its ID.
        /// This method checks if the group name exists in the database and removes it if found.
        /// It takes the class ID as input and performs the deletion asynchronously.
        /// </summary>
        public async Task DeleteGroupName(string groupnameid)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.GroupNameRepository.DeleteAsync(groupnameid);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi xóa lớp học", ex);
            }
        }

        /// <summary>
        /// Retrieve all group names (classes) from the database.
        /// This method returns a list of all group names stored in the database.
        /// </summary>
        public async Task<List<GroupClass>> GetAllAsync()
        {
            try
            {
                var groupClass = await _unitOfWork.Repository<GroupClass>().GetAllAsync();
                return groupClass;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách lớp học", ex);
            }
        }

        /// <summary>
        /// Import group names (classes) from an Excel file.
        /// This method takes a list of GroupName objects as input and adds them to the database.
        /// </summary>
        public async Task ImportGroupNameFromExcel(List<GroupClass> listGroupNameFromExcel)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                foreach (var groupname in listGroupNameFromExcel)
                {
                    var existing = await _unitOfWork.GroupNameRepository.CheckGroupNameExistsAsync(groupname.GroupName);
                    if (!existing)
                    {
                        await _unitOfWork.GroupNameRepository.AddAsync(groupname);
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
        /// Retrieve a group name (class) by its class ID.
        /// </summary>
        /// <param name="classID"></param>
        public async Task<GroupClass?> GetByGroupNameIdAsync(string groupnameId)
        {
            return await _unitOfWork.GroupNameRepository.GetGroupNameAsync(groupnameId);
        }

        /// <summary>
        /// Update an existing group name (class) in the database.
        /// This method takes a GroupName object as input, updates the corresponding record in the database, and saves the changes asynchronously.
        /// </summary>
        public async Task UpdateGroupName(GroupClass groupname)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existinggroupname = await GetByGroupNameIdAsync(groupname.GroupName);
                if (existinggroupname != null)
                {
                    _mapper.Map(groupname, existinggroupname);
                    await _unitOfWork.Repository<GroupClass>().UpdateAsync(existinggroupname);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception("Lỗi khi cập nhật lớp học", ex);
            }
        }

        /// <summary>
        /// Check if a class ID exists in the database.
        /// This method takes a class ID as input and returns a boolean indicating whether the class ID exists.
        /// </summary>
        public async Task<bool> CheckGroupNameExistsAsync(string groupname)
        {
            try
            {
                return await _unitOfWork.GroupNameRepository.CheckGroupNameExistsAsync(groupname);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi kiểm tra mã lớp", ex);
            }
        }

        /// <summary>
        ///  Reads group class names from an Excel file and returns a list of GroupClass objects.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<GroupClass> ReadGroupNameFromExcel(string filePath)
        {
            var groupnames = new List<GroupClass>();
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

                    Utility.IsEmptyExcelRow(worksheet, rowCount, colCount);
                    var listColCheck = new List<int> { 1 };
                    Utility.IsDuplicatedExcelRow(worksheet, rowCount, listColCheck);

                    for (int c = 1; c <= colCount; c++)
                    {
                        string header = worksheet[1, c].Value?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(header))
                            headerMap[header] = c;
                    }

                    string[] requiredHeaders = { "GroupName", "Khóa", "Kỳ", "BM", "Ngành", "Tổ chức", "Buổi" };

                    foreach (var h in requiredHeaders)
                        if (!headerMap.ContainsKey(h))
                            throw new Exception($"Missing required column: {h}");

                    for (int r = 2; r <= rowCount; r++)
                    {
                        var groupname = new GroupClass
                        {
                            GroupName = worksheet[r, headerMap["GroupName"]].Value,
                            CurriculumCode = worksheet[r, headerMap["Khóa"]].Value,
                            Department = worksheet[r, headerMap["BM"]].Value,
                            Major = worksheet[r, headerMap["Ngành"]].Value,
                            Term = int.TryParse(worksheet[r, headerMap["Kỳ"]].Value, out int term) ? term : 0,
                            TeachingMode = worksheet[r, headerMap["Tổ chức"]].Value,
                            PartOfDayInTheFirstTerm = worksheet[r, headerMap["Buổi"]].Value
                        };
                        groupnames.Add(groupname);
                    }
                }
            }
            return groupnames;
        }

        /// <summary>
        /// Exports a list of group names (classes) to an Excel file.
        /// This method creates an Excel file at the specified file path and writes the provided group name data into it.
        /// The Excel file will contain columns for GroupName, Khóa, Ngành, BM, and Kỳ.
        /// </summary>
        /// <param name="groupNames">The list of GroupClass objects to export.</param>
        /// <param name="filePath">The file path where the Excel file will be saved.</param>
        public void ExportToExcelGroupName(List<GroupClass> groupNames, string filePath)
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];
            // Header
            sheet[1, 1].Text = "GroupName";
            sheet[1, 2].Text = "Khóa";
            sheet[1, 3].Text = "Ngành";
            sheet[1, 4].Text = "BM";
            sheet[1, 5].Text = "Kỳ";
            sheet[1, 6].Text = "Tổ chức";
            sheet[1, 7].Text = "Buổi";
            int row = 2;
            foreach (var groupName in groupNames)
            {
                sheet[row, 1].Text = groupName.GroupName ?? "";
                sheet[row, 2].Text = groupName.CurriculumCode ?? "";
                sheet[row, 3].Text = groupName.Major ?? "";
                sheet[row, 4].Text = groupName.Department ?? "";
                sheet[row, 5].Number = groupName.Term ?? 0;
                sheet[row, 6].Text = groupName.TeachingMode ?? "";
                sheet[row, 7].Text = groupName.PartOfDayInTheFirstTerm ?? "";
                row++;
            }
            workbook.SaveAs(filePath);
        }

        public async Task <List<string>> GetAllMajorAsync()
        {
            try
            {
                return await _unitOfWork.GroupNameRepository.GetAllMajorAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách ngành học", ex);
            }
        }
        #endregion
    }
}
