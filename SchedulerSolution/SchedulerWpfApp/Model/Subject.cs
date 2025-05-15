namespace SchedulerWpfApp.Model
{
    public class Subject
    {
        public string SubjectCode { get; set; }
        public string? SubjectName { get; set; }
        public string? Major { get; set; }
        public int TotalSessions { get; set; }
        public int SlotsPerWeek { get; set; }
        public string? SemesterId { get; set; }
        public List<LecturerSubject>? LecturerSubjects { get; set; }

        public Subject(string subjectCode, string subjectName, string major, int numberOfSlots, int numberOfSlotsPerWeek, string semesterId, List<LecturerSubject>? lecturerSubjects = null)
        {
            SubjectCode = subjectCode;
            SubjectName = subjectName;
            Major = major;
            TotalSessions = numberOfSlots;
            SlotsPerWeek = numberOfSlotsPerWeek;
            SemesterId = semesterId;
            LecturerSubjects = lecturerSubjects ?? new List<LecturerSubject>();
        }
    }
}
