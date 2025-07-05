using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices
{
    public interface ILecturerSubjectServices
    {
        Task ImportLecturerSubjectFromExcel(List<LecturerSubject> listLectuerSubjectFromExcel);
        Task<List<LecturerSubject>> GetAllAsync();
        Task<LecturerSubject> GetByIdAsync(int id);
        Task<List<LecturerSubject>> GetBySubjectCodeAsync(string subjectCode);
        Task AddAsync(LecturerSubject lecturerSubject);
        Task UpdateAsync(LecturerSubject lecturerSubject);
        Task DeleteAsync(int id);
        List<LecturerSubject> ReadLecturerSubjectFromExcel(string filePath);
        void ExportToLecturerSubjectExcel(List<LecturerSubject> lectureSubjects, string filePath);
        Task<LecturerSubject> CheckLecturerSubjectExits(LecturerSubject lecturerSubject);
    }
}
