using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.LecturerServices
{
    public interface ILecturerServices
    {
        Task<List<Lecturer>> GetAllLecturerAsync();
        Task<Lecturer?> GetByLectureCodeAsync(string lectureCode);
        Task AddLecture(Lecturer lecture);
        Task UpdateLecture(Lecturer lecture);
        Task DeleteLecture(string lectureId);
        Task ImportLectureFromExcel(List<Lecturer> listLectureFromExcel);
        void ExportToExcel(List<Lecturer> lecturers, string filePath);
        List<Lecturer> ReadLecturersFromExcel(string filePath);
    }
}
