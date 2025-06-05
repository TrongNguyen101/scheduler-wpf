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
        Task<List<GroupClass>> GetAllAsync();

        // Lấy thông tin một lớp học theo ID (dạng số nguyên)
        Task<GroupClass?> GetByIdAsync(int id);

        // Thêm một lớp học mới vào hệ thống
        Task AddGroupName(GroupClass groupName);

        // Cập nhật thông tin lớp học hiện tại
        Task UpdateGroupName(GroupClass person);

        // Xóa lớp học theo mã lớp (ClassId)
        Task DeleteGroupName(string classid);

        // Nhập danh sách lớp học từ file Excel
        Task ImportGroupNameFromExcel(List<GroupClass> listGroupNameFromExcel);

        // Kiểm tra mã lớp đã tồn tại trong hệ thống hay chưa (true = đã tồn tại)
        Task<bool> CheckClassIdExistsAsync(string classId);


    }
}
