using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm.DTO
{
    public class SchedulePartOfDayContext
    {
        public List<GroupClass> GroupNames { get; set; }
        public Dictionary<string, List<LecturerSubject>> LecturersTeachSubjects { get; set; }
        public string PartOfDay { get; set; } // "AM" or "PM"
        public DateTime StartDate { get; set; } // Start date of the schedule
        public List<TreeForSchedule> TreeForSchedules { get; set; } // List of schedules for the part of the day
    }
}
