using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices
{
    public interface ILecturerSubjectServices
    {
        Task ImportLectureSubjectFromExcel(List<LecturerSubject> listlecturesubjectFromExcel);
        Task<List<LecturerSubject>> GetAllAsync();
        Task<LecturerSubject> GetByIdAsync(int id);
        Task AddAsync(LecturerSubject lecturerSubject);
        Task UpdateAsync(LecturerSubject lecturerSubject);
        Task DeleteAsync(int id);
        List<LecturerSubject> ReadLectureSubjectFromExcel(string filePath);
        void ExportToLectureSubjectExcel(List<LecturerSubject> lectureSubjects, string filePath);
    }
}
