namespace SchedulerWpfApp.Model
{
    public class Schedule
    {
        public int ScheduleId { get; set; } // Id of schedule 
        public string? RoomNo { get; set; } // Room number
        public string? PartOfDay { get; set; } // Session of the schedule in a day (AM -PM)
        public string? SlotTime { get; set; } // Time of the schedule in a day (1, 2, 3, 4)
        public string? StatusSlot { get; set; } // Status of the schedule (Online - Offline)
        public DateTime? Date { get; set; } // Date of the schedule
        public string? Major { get; set; } // Major of the class
        public string? SubjectCode { get; set; } // Subject code of the schedule
        public string? GroupName { get; set; } // Class name of the schedule
        public string? LecturerId { get; set; } // Lecturer id of the schedule
        public string? TypeSlot { get; set; } // Type of the slot (New Slot - Old Slot)
        public int SessionNo { get; set; } // order of slot (1, 2, 3, 4, 5, 6, 7, 8, 9,...)
        // Slot type code of the schedule
        // (A24 meaning: A: session in day(AM -PM), 2: slot 1 moday, 4: slot 2 Wednesday)
        // (A42 meaning: A: session in day(AM -PM), 4: slot 1 Wednesday, 2: slot 2 Moday)                                             
        // (P62 meaning: P: session in day(AM -PM), 6: slot 3 Friday, 2: slot 4 Monday)                                             
        // (P46 meaning: P: session in day(AM -PM), 4: slot 3 Wednesday, 6: slot 4 Friday)                                                                                      
        public string? SlotTypeCode { get; set; } 

        public Schedule(string roomNo, string partOfDay, string slotTime, string statusSlot, string subjectCode, DateTime date, string groupName, string lecturerId, string slotTypeCode, string typeSlot, int sessionNo)
        {
            RoomNo = roomNo;
            PartOfDay = partOfDay;
            SlotTime = slotTime;
            StatusSlot = statusSlot;
            Date = date;
            Major = null;
            SubjectCode = subjectCode;
            GroupName = groupName;
            LecturerId = lecturerId;
            SlotTypeCode = slotTypeCode;
            TypeSlot = typeSlot;
            SessionNo = sessionNo;
        }
    }
}
