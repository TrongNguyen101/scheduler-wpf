using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Person")]
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("FirstName")]
        public string? FirstName { get; set; }
        [Column("LastName")]
        public string? LastName { get; set; }
        [Column("Email")]
        public string? Email { get; set; }
        [Column("Phone")]
        public string? Phone { get; set; }
    }
}
