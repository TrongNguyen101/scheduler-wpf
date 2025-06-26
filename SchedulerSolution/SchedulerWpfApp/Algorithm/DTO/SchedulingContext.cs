using SchedulerWpfApp.Model;
namespace SchedulerWpfApp.Algorithm.DTO
{
    public class SchedulingContext
    {
        public List<Room> Rooms { get; set; }
        public List<CurriculumSubject> CurriculumSubjects { get; set; }
        public List<Lecturer> Lecturers { get; set; }
        public List<GroupClass> GroupNames { get; set; }
        public ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> CurriculumLookup { get; set; }
        public Dictionary<string, List<LecturerSubject>> LecturersTeachSubjects { get; set; }
    }
}
