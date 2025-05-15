using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public class GroupNameService : IGroupNameService
    {
        private readonly DataContext _context;

        public GroupNameService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Thêm lớp học mới vào database
        /// </summary>
        public async Task AddGroupName(GroupName groupName)
        {
            try
            {
                _context.GroupName.Add(groupName);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi thêm lớp học", ex);
            }
        }

        /// <summary>
        /// Xóa lớp học theo mã lớp
        /// </summary>
        public async Task DeleteGroupName(string classid)
        {
            try
            {
                var existing = await _context.GroupName.FindAsync(classid);
                if (existing != null)
                {
                    _context.GroupName.Remove(existing);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi xóa lớp học", ex);
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả lớp học
        /// </summary>
        public async Task<List<GroupName>> GetAllAsync()
        {
            try
            {
                return await _context.GroupName.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách lớp học", ex);
            }
        }

        /// <summary>
        /// Chưa triển khai - dùng để lấy lớp theo ID số nếu cần (không dùng nếu khóa là string)
        /// </summary>
        public Task<GroupName?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Nhập danh sách lớp học từ file Excel
        /// </summary>
        public async Task ImportGroupNameFromExcel(List<GroupName> listGroupNameFromExcel)
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
        /// Cập nhật thông tin lớp học
        /// </summary>
        public async Task UpdateGroupName(GroupName person)
        {
            try
            {
                _context.GroupName.Update(person);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi cập nhật lớp học", ex);
            }
        }

        /// <summary>
        /// Kiểm tra xem mã lớp đã tồn tại hay chưa
        /// </summary>
        public async Task<bool> CheckClassIdExistsAsync(string classId)
        {
            try
            {
                return await _context.GroupName.AnyAsync(g => g.ClassId == classId);
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi kiểm tra mã lớp", ex);
            }
        }
    }
}
