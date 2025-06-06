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

        public string LecturerId { get; set; }

        public string SubjectCode { get; set; }

        [Column("SubjectName")]
        public string? SubjectName { get; set; }

        [Column("LecturerName")]
        public string? LecturerName { get; set; }

        [Column("Major")]
        public string? Major { get; set; }

        [Column("Term")]
        public string? Term { get; set; }

        [Column("NumberOfClasses")]
        public int? NumberOfClasses { get; set; }

        [Column("TotalSLots")]
        public int? TotalSlots { get; set; }

        public Subject? Subject { get; set; }
        public Lecturer? Lecturer { get; set; }
    }
}
