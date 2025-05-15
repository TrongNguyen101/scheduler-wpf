namespace SchedulerWpfApp.Model
{
    public class LecturerSubject
    {
        public string LecturerId { get; set; }
        public string SubjectCode { get; set; }
        public string? LecturerName { get; set; }
        public int NumberOfClasses { get; set; }
        public Subject? Subject { get; set; }
        public Lecturer? Lecturer { get; set; }

        public LecturerSubject(string lecturerId, string subjectCode, string lecturerName, int numberOfClasses)
        {
            LecturerId = lecturerId;
            SubjectCode = subjectCode;
            NumberOfClasses = numberOfClasses;
            LecturerName = lecturerName;
        }
    }
}
