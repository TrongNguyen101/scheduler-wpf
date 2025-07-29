
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class RoomSchedulerOnOff
    {
        // --- Các danh sách Major để phân loại ---
        private readonly HashSet<string> listMajorGroupA = new() { "FN", "HM", "MC", "BA", "TM", "IB", "EC" };
        private readonly HashSet<string> listMajorGroupB = new() { "AI", "SE", "JL", "KR", "EL" };

        // --- Hàm chính để điều phối ---
        public void AssignAndRotateSchedules(List<Schedule> allSchedules, List<Room> allRooms, List<GroupClass> allGroupClasses)
        {
            // Tạo map để tra cứu thông tin GroupClass nhanh chóng
            var groupClassMap = allGroupClasses.ToDictionary(gc => gc.GroupName);
            var allScheduleGroups = allSchedules.ToLookup(s => s.GroupName);

            // 1. Phân loại các GroupName thành 3 nhóm: A, B, và Khác
            var groupNamesInA = new List<string>();
            var groupNamesInB = new List<string>();

            foreach (var groupName in allScheduleGroups)
            {
                if (groupClassMap.TryGetValue(groupName.Key, out var groupInfo))
                {
                    if (listMajorGroupA.Contains(groupInfo.Major)) groupNamesInA.Add(groupName.Key);
                    else if (listMajorGroupB.Contains(groupInfo.Major)) groupNamesInB.Add(groupName.Key);
                }
            }

            // 2. Ghép cặp các nhóm A và B
            var pairedAssignments = new Dictionary<string, (string Partner, Room Room)>(); // Key: GroupName, Value: (Partner, SharedRoom)

            int pairingCount = Math.Min(groupNamesInA.Count, groupNamesInB.Count);
            for (int i = 0; i < pairingCount; i++)
            {
                pairedAssignments[groupNamesInA[i]] = (groupNamesInB[i], null); // Tạm thời chưa có phòng
                pairedAssignments[groupNamesInB[i]] = (groupNamesInA[i], null);
            }


            // 3. Xếp phòng cho các cặp và các nhóm đơn lẻ
            AssignRoomsToEntities(pairedAssignments, allScheduleGroups, allRooms);

            // 4. Xác định trạng thái Online/Offline và gán phòng cuối cùng
            UpdateScheduleStatus(allSchedules, pairedAssignments);
        }

        // --- Các hàm trợ giúp ---

        private void AssignRoomsToEntities(
            Dictionary<string, (string Partner, Room Room)> pairedAssignments,
            ILookup<string, Schedule> allScheduleGroups,
            List<Room> allRooms)
        {
            // Tạm thời coi mọi thực thể đều cần 1 phòng, sau đó sẽ gán phòng chung cho cặp
            // ... (Logic phức tạp để ưu tiên phòng tòa G cho các cặp/nhóm có BIT/BBA)
            // Để đơn giản, ví dụ này sẽ xếp tuần tự
            var availableRooms = new Queue<Room>(allRooms);

            // Gán phòng cho các cặp
            var processedPartners = new HashSet<string>();
            foreach (var groupName in pairedAssignments.Keys)
            {
                if (processedPartners.Contains(groupName) || !availableRooms.TryDequeue(out var room)) continue;

                var partnerName = pairedAssignments[groupName].Partner;
                pairedAssignments[groupName] = (partnerName, room);
                pairedAssignments[partnerName] = (groupName, room);
                processedPartners.Add(partnerName);
            }
        }

        private void UpdateScheduleStatus(
            List<Schedule> allSchedules,
            Dictionary<string, (string Partner, Room Room)> finalAssignments)
        {
            int startWeekNumber = 1; // Giả sử tuần bắt đầu là tuần 1

            foreach (var schedule in allSchedules)
            {
                if (!finalAssignments.TryGetValue(schedule.GroupName, out var assignment)) continue;

                // Logic luân phiên cho các nhóm được ghép cặp
                bool isGroupA = listMajorGroupA.Contains(allSchedules.First(s => s.GroupName == schedule.GroupName).Major);
                int currentWeekNumber = (schedule.Date.GetValueOrDefault().DayOfYear / 7) + startWeekNumber;
                bool isWeekEven = currentWeekNumber % 2 == 0;
                bool isDayOdd = schedule.Date.GetValueOrDefault().DayOfWeek == DayOfWeek.Monday ||
                                schedule.Date.GetValueOrDefault().DayOfWeek == DayOfWeek.Wednesday ||
                                schedule.Date.GetValueOrDefault().DayOfWeek == DayOfWeek.Friday;

                bool shouldBeOffline = false;
                if (isWeekEven) // Tuần chẵn (2, 4...)
                {
                    shouldBeOffline = isDayOdd ? !isGroupA : isGroupA;
                }
                else // Tuần lẻ (1, 3...)
                {
                    shouldBeOffline = isDayOdd ? isGroupA : !isGroupA;
                }

                schedule.StatusSlot = shouldBeOffline ? ScheduleConstants.StatusSlotIsOffline : ScheduleConstants.StatusSlotIsOnline;
                schedule.RoomId = shouldBeOffline ? assignment.Room.RoomId : null;
                schedule.RoomName = shouldBeOffline ? assignment.Room.RoomName : null;

            }
        }
    }
}
