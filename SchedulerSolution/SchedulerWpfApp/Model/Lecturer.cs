namespace SchedulerWpfApp.Model
{
    public class Lecturer
    {
        public string LecturerId { get; set; }
        public string? LecturerName { get; set; }
        public string? Role { get; set; }
        public List<LecturerSubject>? LecturerSubjects { get; set; }

        public Lecturer(string lecturerId, string lecturerName, string? role List<LecturerSubject>? lecturerSubjects = null)
        {
            LecturerId = lecturerId;
            LecturerName = lecturerName;
            Role = role;
            LecturerSubjects = lecturerSubjects ?? new List<LecturerSubject>();
        }
    }
}
