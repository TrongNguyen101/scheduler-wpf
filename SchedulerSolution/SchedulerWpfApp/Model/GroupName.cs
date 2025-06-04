using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("GroupName")]
    public class GroupName
    {
        [Key]
        public string ClassId { get; set; } = string.Empty;

        [Column("Course")]
        public string? Course { get; set; }

        [Column("Major")]
        public string? Major { get; set; }

        [Column("Term")]
        public int Term { get; set; }

        [Column("Department")]
        public string? Department { get; set; }
    }
}
