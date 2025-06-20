using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.ServiceRefactor.LecturerServices
{
    public interface ILecturerServices
    {
        Task<List<Lecturer>> GetAllLecturerAsync();
        Task<Lecturer?> GetByLecturerCodeAsync(string lecturerCode);
        Task AddLecturer(Lecturer lecturer);
        Task UpdateLecturer(Lecturer lecturer);
        Task DeleteLecturer(string lecturerId);
        Task ImportLecturerFromExcel(List<Lecturer> listLecturerFromExcel);
        void ExportToExcel(List<Lecturer> lecturers, string filePath);
        List<Lecturer> ReadLecturersFromExcel(string filePath);
    }
}
