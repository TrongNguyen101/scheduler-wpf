using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("CurriculumSubject")]
    public class CurriculumSubject
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("CurriculumCode")]
        public int CurriculumCode { get; set; }

        [Column("SubjectCode")]
        [StringLength(50)]
        public string SubjectCode { get; set; }

        [Column("SubjectNameEnglish")]
        [StringLength(255)]
        public string SubjectNameEnglish { get; set; }

        [Column("SubjectNameVietnamese")]
        [StringLength(255)]
        public string? SubjectNameVietnamese { get; set; }

        [Column("TermNo")]
        public int TermNo { get; set; }

        [Column("IsCombo")]
        public bool IsCombo { get; set; } = false;

        [Column("Credit")]
        public int Credit { get; set; }

        // === Navigation properties ===

        public Subject? Subject { get; set; }

        public Curriculum? Curriculum { get; set; }
    }
}
