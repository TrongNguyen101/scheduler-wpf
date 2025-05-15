using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("LecturerSubject")]
    public class LecturerSubject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Lecturer")]
        public string LecturerId { get; set; }
        [ForeignKey("Subject")]
        public string SubjectCode { get; set; }
        [Column("LecturerName")]
        public string? LecturerName { get; set; }
        [Column("NumberOfClasses")]
        public int NumberOfClasses { get; set; }
        public virtual Subject? Subject { get; set; }
        public virtual Lecturer? Lecturer { get; set; }

        public LecturerSubject() { }

        public LecturerSubject(string lecturerId, string subjectCode, string lecturerName, int numberOfClasses)
        {
            LecturerId = lecturerId;
            SubjectCode = subjectCode;
            NumberOfClasses = numberOfClasses;
            LecturerName = lecturerName;
        }
    }
}
