using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Room")]

    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("RoomId")]
        public int RoomId { get; set; } // Id of schedule 
        [Column("RoomName")]

        public string? RoomName { get; set; }
        [Column("TotalPersons")]

        public int? TotalPersons { get; set; }
        [Column("TypeOfRoom")]

        public string? TypeOfRoom { get; set; }
    }
}
