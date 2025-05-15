using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Task AddGroupName(GroupName groupName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteGroupName(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<GroupName>> GetAllAsync()
        {
            return await _context.GroupName.ToListAsync();
        }

        public Task<GroupName?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task ImportGroupNameFromExcel(List<GroupName> listGroupNameFromExcel)
        {
            foreach (var room in listGroupNameFromExcel)
            {
                _context.GroupName.Add(room);
            }
            await _context.SaveChangesAsync();
        }

        public Task UpdateGroupName(GroupName person)
        {
            throw new NotImplementedException();
        }
    }

}
