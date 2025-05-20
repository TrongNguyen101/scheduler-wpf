using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
{
    public interface IGroupNameService
    {
        // Lấy danh sách tất cả các lớp học
        Task<List<GroupName>> GetAllAsync();

        // Lấy thông tin một lớp học theo ID (dạng số nguyên)
        Task<GroupName?> GetByIdAsync(int id);

        // Thêm một lớp học mới vào hệ thống
        Task AddGroupName(GroupName groupName);

        // Cập nhật thông tin lớp học hiện tại
        Task UpdateGroupName(GroupName person);

        // Xóa lớp học theo mã lớp (ClassId)
        Task DeleteGroupName(string classid);

        // Nhập danh sách lớp học từ file Excel
        Task ImportGroupNameFromExcel(List<GroupName> listGroupNameFromExcel);

        // Kiểm tra mã lớp đã tồn tại trong hệ thống hay chưa (true = đã tồn tại)
        Task<bool> CheckClassIdExistsAsync(string classId);


    }
}
