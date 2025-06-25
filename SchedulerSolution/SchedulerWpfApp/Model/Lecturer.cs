using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Lecturer")]
    public class Lecturer
    {
        [Key]
        [Column("LecturerId")]
        public string LecturerId { get; set; }

        [Column("LecturerAccount")]
        public string? LecturerAccount { get; set; }

        [Column("LecturerName")]
        public string? LecturerName { get; set; }

        [Column("Role")]
        public string? Role { get; set; }

        [Column("Department")]
        public string? Department { get; set; }

        public ICollection<LecturerSubject>? LecturerSubjects { get; set; }

        public ICollection<Schedule>? Schedules { get; set; }

        public ICollection<LecturerRequest>? LecturerRequests { get; set; }
    }
}
