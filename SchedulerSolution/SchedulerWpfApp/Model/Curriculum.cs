using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Curriculum")]
    public class Curriculum
    {
        [Key]
        [Column("CurriculumCode")]
        public string CurriculumCode { get; set; } // Unique code for the curriculum

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        public ICollection<CurriculumSubject>? CurriculumSubjects { get; set; } // Collection of subjects in the curriculum
    }
}
