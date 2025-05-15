using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Subject")]
    public class Subject
    {
        [Key]
        [Column("SubjectCode")]
        public string SubjectCode { get; set; }

        [Column("SubjectName")]
        public string? SubjectName { get; set; }

        [Column("Major")]
        public string? Major { get; set; }

        [Column("TotalSessions")]
        public int TotalSessions { get; set; }

        [Column("SlotsPerWeek")]
        public int SlotsPerWeek { get; set; }

        [Column("SemesterId")]
        public string? SemesterId { get; set; }

        public virtual List<LecturerSubject>? LecturerSubjects { get; set; }
        public Subject() { }

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
