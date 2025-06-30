using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("GroupClass")]
    public class GroupClass
    {
        [Key]
        public string GroupName { get; set; } = string.Empty;

        [Column("CurriculumCode")]
        public string? CurriculumCode { get; set; }

        [Column("Major")]
        public string? Major { get; set; }

        [Column("Department")]
        public string? Department { get; set; }

        [Column("Term")]
        public int? Term { get; set; }

        [Column("TeachingMode ")]
        public string? TeachingMode { get; set; }// Onl/Off, Off, Onl, OJT...


        public ICollection<Schedule>? Schedules { get; set; }
    }
}
