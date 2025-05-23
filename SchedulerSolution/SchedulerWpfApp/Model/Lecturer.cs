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

        [Column("LecturerName")]
        public string? LecturerName { get; set; }

        [Column("Role")]
        public string? Role { get; set; }

        public virtual List<LecturerSubject>? LecturerSubjects { get; set; }
    }
}
