using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SchedulerWpfApp.Model
{
    [Table("Schedules")]
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScheduleId { get; set; } // Id of schedule 

        [Column("RoomId")]
        public int? RoomId { get; set; } // Room number

        [Column("RoomName")]
        public string? RoomName { get; set; } // Room number

        [Column("PartOfDay")]
        public string? PartOfDay { get; set; } // Session of the schedule in a day (AM - PM)

        [Column("SlotTime")]
        public int? SlotTime { get; set; } // Time of the schedule in a day (1, 2, 3, 4)

        [Column("StatusSlot")]
        public string? StatusSlot { get; set; } // Status of the schedule (Online - Offline)

        [Column("Date")]
        public DateTime? Date { get; set; } // Date of the schedule

        [Column("Major")]
        public string? Major { get; set; } // Major of the class

        [Column("SubjectCode")]
        public string? SubjectCode { get; set; } // Subject code of the schedule

        [Column("GroupName")]
        public string? GroupName { get; set; } // Class name of the schedule

        [Column("LecturerId")]
        public string? LecturerId { get; set; } // Lecturer id of the schedule

        [Column("LecturerName")]
        public string? LecturerName { get; set; } // Lecturer name of the schedule

        [Column("LecturerAccount")]
        public string? LecturerAccount { get; set; } // Lecturer name of the schedule

        [Column("TypeSlot")]
        public string? TypeSlot { get; set; } // Type of the slot (New Slot - Old Slot)

        [Column("SessionNo")]
        public int? SessionNo { get; set; } // Order of slot (1, 2, 3, 4, 5, 6, 7, 8, 9,...)

        // Slot type code of the schedule
        // (A24 meaning: A: session in day(AM -PM), 2: slot 1 Monday, 4: slot 2 Wednesday)
        // (A42 meaning: A: session in day(AM -PM), 4: slot 1 Wednesday, 2: slot 2 Monday)
        // (P62 meaning: P: session in day(AM -PM), 6: slot 3 Friday, 2: slot 4 Monday)
        // (P46 meaning: P: session in day(AM -PM), 4: slot 3 Wednesday, 6: slot 4 Friday)
        [Column("SlotTypeCode")]
        public string? SlotTypeCode { get; set; }

        [Column("TermInYear")]
        public string? TermInYear { get; set; } // Type of the slot (Normal - Combo)]

        /// <summary>
        /// Gets a value indicating whether this schedule is assigned to a lecturer.
        /// A schedule is considered assigned if it has LecturerId (primary check).
        /// </summary>
        [NotMapped]
        public bool IsAssigned => !string.IsNullOrEmpty(LecturerId);

        /// <summary>
        /// Gets a value indicating whether this schedule needs urgent assignment.
        /// A schedule needs urgent assignment if both LecturerId and LecturerAccount are null or empty.
        /// </summary>
        [NotMapped]
        public bool NeedsUrgentAssignment => string.IsNullOrEmpty(LecturerId) && string.IsNullOrEmpty(LecturerAccount) && string.IsNullOrEmpty(LecturerName);

        /// <summary>
        /// Gets the assignment status text for display purposes.
        /// </summary>
        [NotMapped]
        public string AssignmentStatusText
        {
            get
            {
                if (IsAssigned)
                {
                    // Hiển thị thông tin giảng viên nếu có
                    if (!string.IsNullOrEmpty(LecturerName))
                        return LecturerName;
                    if (!string.IsNullOrEmpty(LecturerAccount))
                        return LecturerAccount;
                    if (!string.IsNullOrEmpty(LecturerId))
                        return LecturerId;
                    return "Đã phân công";
                }

                if (NeedsUrgentAssignment)
                    return "🚨 CẦN PHÂN CÔNG GẤP";

                return "⚠️ CHƯA PHÂN CÔNG";
            }
        }

        public Schedule Clone()
        {
            return (Schedule)this.MemberwiseClone();
        }
    }
}
