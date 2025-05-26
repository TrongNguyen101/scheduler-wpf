using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("LecturerRequests")]
    public class LecturerRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("LecturerId")]
        public string? LecturerId { get; set; }

        [Required]
        [Column("DayName")]
        public string? DayName { get; set; }

        [Required]
        [Column("Session")]
        public string? Session { get; set; }

        [Column("SlotTime")]
        public string? SlotTime { get; set; }

        [Column("SlotType")]
        public string? SlotType { get; set; }
    }
}