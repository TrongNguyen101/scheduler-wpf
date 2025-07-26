namespace SchedulerWpfApp.Algorithm.DTO
{
    public static class ScheduleConstants
    {
        /// <summary>
        /// Danh sách các tuần đặc biệt (tuần đầu và cuối), thường có lịch học offline.
        /// </summary>
        public static readonly int[] FirstAndFinalWeeks = { 1, 10 };

        /// <summary>
        /// Dải các tuần học thông thường ở giữa kỳ (từ tuần 2 đến 9).
        /// </summary>
        public static readonly IEnumerable<int> MidTermWeeks = Enumerable.Range(2, 8);

        public const int DaysInWeek = 7;
        public const int SlotsPerSession = 2;

        public const string TypeSlotIsNew = "NEW SLOT";

        public const string TypeSlotIsOld = "OLD SLOT";

        public const string StatusSlotIsOnline = "ON"; 

        public const string StatusSlotIsOffline = "OFF";

        public const string TechingModeIsOnOff = "ON/OFF";

        public const string TechingModeIsFullOff = "OFF";

        public const string TechingModeIsOJT = "OJT";


        public const string TechingModeIsCoursera = "C-ON";

        public const string PartOfDayIsAM = "A";
        public const string PartOfDayIsPM = "P";

        public const int NewSlotStartTimeAM = 1; // new slot start time in AM is 1
        public const int NewSlotStartTimePM = 3; // new slot strart time in PM is 3

        public const int TotalSlotsNomal = 20;
    }
}
