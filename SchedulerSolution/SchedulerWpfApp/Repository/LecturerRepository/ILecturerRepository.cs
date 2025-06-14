using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerRepository
{
    public interface ILecturerRepository : IBaseRepository<Lecturer>
    {
        Task<Lecturer?> GetByLectureCodeAsync(string LecturerId);
        Task DeleteLecture(string LecturerId);
    }
}
