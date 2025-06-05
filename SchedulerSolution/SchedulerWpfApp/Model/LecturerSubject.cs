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
        [ForeignKey("LecturerId")]
        public string LecturerId { get; set; }
        [ForeignKey("SubjectCode")]
        public string SubjectCode { get; set; }
        [Column("LecturerName")]
        public string? LecturerName { get; set; }
        [Column("NumberOfClasses")]
        public int NumberOfClasses { get; set; }
        public Subject? Subject { get; set; }
        public Lecturer? Lecturer { get; set; }
    }
}
