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
        Task<List<GroupName>> GetAllAsync();
        Task<GroupName?> GetByIdAsync(int id);
        Task AddGroupName(GroupName groupName);
        Task UpdateGroupName(GroupName person);
        Task DeleteGroupName(int id);
        Task ImportGroupNameFromExcel(List<GroupName> listGroupNameFromExcel);
    }
}
