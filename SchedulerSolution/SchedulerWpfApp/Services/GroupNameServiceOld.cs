using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;
using AutoMapper;
using SchedulerWpfApp.Repository;
namespace SchedulerWpfApp.Services
{
    /// <summary>
    /// Service class for managing group names (classes)
    /// This class provides methods to add, delete, update, import from Excel, and check existence of group names in the database.
    ///  It also includes methods to retrieve all group names and check if a class ID exists.
    /// </summary>
    public class GroupNameServiceOld : IGroupNameServiceOld
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public GroupNameServiceOld(DataContext context, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _context = context;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Create a new instance of GroupNameService with the provided DataContext.
        /// This method adds a new group name (class) to the database.
        /// It takes a GroupName object as input and saves it asynchronously.
        /// </summary>
        /// <param name="groupName"></param>

        public async Task AddGroupName(GroupClass groupName)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Repository<GroupClass>().AddAsync(groupName);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm lớp học", ex);
            }
        }

        /// <summary>
        /// Delete a group name (class) by its ID.
        /// This method checks if the group name exists in the database and removes it if found.
        /// It takes the class ID as input and performs the deletion asynchronously.
        /// </summary>
        public async Task DeleteGroupName(string classid)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.GroupNameRepository.DeleteAsync(classid);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
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
        /// Retrieve a group name (class) by its ID.
        /// This method searches for a group name in the database using its unique identifier.
        /// It takes the class ID as input and returns the corresponding GroupName object if found.
        /// </summary>
        public Task<GroupClass?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Import group names (classes) from an Excel file.
        /// This method takes a list of GroupName objects as input and adds them to the database.
        /// </summary>
        public async Task ImportGroupNameFromExcel(List<GroupClass> listGroupNameFromExcel)
        {
            try
            {
                foreach (var room in listGroupNameFromExcel)
                {
                    _context.GroupName.Add(room);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi nhập lớp học từ Excel", ex);
            }
        }

        /// <summary>
        /// Retrieve a group name (class) by its class ID.
        /// </summary>
        /// <param name="classID"></param>
        public async Task<GroupClass?> GetByGroupNameCodeAsync(string classID)
        {
            return await _unitOfWork.GroupNameRepository.GetGroupClassByCodeAsync(classID);
        }

        /// <summary>
        /// Update an existing group name (class) in the database.
        /// This method takes a GroupName object as input, updates the corresponding record in the database, and saves the changes asynchronously.
        /// </summary>
        public async Task UpdateGroupName(GroupClass groupname)
        {
            try
            {
                var existinggroupname = await GetByGroupNameCodeAsync(groupname.GroupName);
                if (existinggroupname != null)
                {
                    _mapper.Map(groupname, existinggroupname);
                    await _unitOfWork.BeginTransactionAsync();
                    await _unitOfWork.Repository<GroupClass>().UpdateAsync(existinggroupname);
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật lớp học", ex);
            }
        }

        /// <summary>
        /// Check if a class ID exists in the database.
        /// This method takes a class ID as input and returns a boolean indicating whether the class ID exists.
        /// </summary>
        public async Task<bool> CheckClassIdExistsAsync(string classId)
        {
            try
            {
                return await _unitOfWork.GroupNameRepository.CheckRoomIdExistsAsync(classId);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi kiểm tra mã lớp", ex);
            }
        }
    }
}
