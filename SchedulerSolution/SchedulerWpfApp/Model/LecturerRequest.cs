namespace SchedulerWpfApp.Model
{
    public class LecturerRequest
    {
        public int Id { get; set; }
        public string? LecturerId { get; set; }
        public string? DayName { get; set; }
        public string? Session { get; set; }
        public string? SlotTime { get; set; }
        public string? SlotType { get; set; }

        public LecturerRequest(int id, string? lecturerId, string? dayName, string? session, string? slotTime, string? slotType)
        {
            Id = id;
            LecturerId = lecturerId;
            DayName = dayName;
            Session = session;
            SlotTime = slotTime;
            SlotType = slotType;
        }
    }
}
