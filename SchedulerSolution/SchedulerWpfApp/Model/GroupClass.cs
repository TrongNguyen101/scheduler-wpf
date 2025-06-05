using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("GroupClass")]
    public class GroupClass
    {
        [Key]
        public string GroupName { get; set; } = string.Empty;

        [Column("Course")]
        public string? Course { get; set; }

        [Column("Major")]
        public string? Major { get; set; }

        [Column("Department")]
        public string? Department { get; set; }

        public ICollection<Schedule>? Schedules { get; set; }
    }
}
