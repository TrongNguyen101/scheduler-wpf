using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        // true tương ứng với new slot, false tương ứng với old slot
        public const bool DefaultSlotStatus = true;
    }
}
