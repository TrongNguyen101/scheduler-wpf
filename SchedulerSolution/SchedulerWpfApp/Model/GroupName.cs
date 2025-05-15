using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("GroupName")]

    public class GroupName
    {
        [Key]
        public string ClassId { get; set; } = string.Empty;
        [Column("Category")]

        public string? Category { get; set; }
        [Column("Major")]

        public string? Major { get; set; }
        [Column("NumberOfStudents")]

        public int NumberOfStudents { get; set; }
        [Column("NumberOfScheduler")]

        public int NumberOfScheduler { get; set; }
    }
}
