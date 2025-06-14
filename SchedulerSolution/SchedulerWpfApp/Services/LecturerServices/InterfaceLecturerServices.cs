using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services.LecturerSubjectServices
{
    public interface InterfaceLecturerServices
    {
        Task<List<Lecturer>> GetAllLecturerAsync();
        Task<Lecturer?> GetByLectureCodeAsync(string lectureCode);
        Task AddLecture(Lecturer lecture);
        Task UpdateLecture(Lecturer lecture);
        Task DeleteLecture(string lectureId);
        Task ImportLectureFromExcel(List<Lecturer> listLectureFromExcel);
    }
}
