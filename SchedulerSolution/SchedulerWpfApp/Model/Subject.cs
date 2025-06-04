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
        public string? SubjectNameEnglish { get; set; }

        [Column("SubjectNameVietnamese")]
        public string? SubjectNameVietnamese { get; set; }

        [Column("TotalTime")]
        public int TotalTime { get; set; }

        [Column("TotalCredits")]
        public int TotalCredits { get; set; }

        [Column("TotalSessions")]
        public int TotalSessions { get; set; }

        [Column("SlotsPerWeek")]
        public int SlotsPerWeek { get; set; }

        public virtual List<LecturerSubject>? LecturerSubjects { get; set; }

        public virtual List<Schedule>? Schedules { get; set; }
    }
}
