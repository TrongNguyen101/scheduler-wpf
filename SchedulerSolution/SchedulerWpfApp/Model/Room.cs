using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchedulerWpfApp.Model
{
    [Table("Room")]

    public class Room
    {
        [Key]
        [Column("RoomId")]
        public int RoomId { get; set; } // Id of schedule 

        [Column("RoomName")]
        public string RoomName { get; set; }

        [Column("TotalPersons")]
        public int TotalPersons { get; set; }

        [Column("TypeOfRoom")]
        public string TypeOfRoom { get; set; }

        [Column("Status")]
        public string Status { get; set; } // Status of the room (Available - Unavailable)

        [Column("Building")]
        public string Building { get; set; } // Building of the room

        [Column("Floor")]
        public int Floor { get; set; } // Floor of the room
    }
}
