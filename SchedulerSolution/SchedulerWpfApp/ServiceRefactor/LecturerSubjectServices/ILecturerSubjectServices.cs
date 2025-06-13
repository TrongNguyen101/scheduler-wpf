using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices
{
    public interface ILecturerSubjectServices
    {
        //Task ImportLectureSubjectFromExcel(List<LecturerSubject> listlecturesubjectFromExcel);
        Task<List<LecturerSubject>> GetAllAsync();
        Task<LecturerSubject> GetByIdAsync(int id);
        Task AddAsync(LecturerSubject lecturerSubject);
        Task UpdateAsync(LecturerSubject lecturerSubject);
        Task DeleteAsync(int id);
    }
}
