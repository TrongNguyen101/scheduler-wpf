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
        //public string? Role { get; set; }
        public virtual List<LecturerSubject>? LecturerSubjects { get; set; }

        public Lecturer() { }
        
        public Lecturer(string lecturerId, string lecturerName, List<LecturerSubject>? lecturerSubjects = null)
        {
            LecturerId = lecturerId;
            LecturerName = lecturerName;
            //Role = role;
            LecturerSubjects = lecturerSubjects ?? new List<LecturerSubject>();
        }
    }
}
