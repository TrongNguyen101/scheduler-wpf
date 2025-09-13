
using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class RoomSchedulerOnOff
    {
        // Danh sách các chuyên ngành cho mỗi nhóm
        private readonly List<string> listMajorGroupA = new List<string> { "FN", "HM", "MC", "BA", "TM", "IB", "EC" };
        private readonly List<string> listMajorGroupB = new List<string> { "AI", "SE", "JL", "KR", "EL" };
        private readonly ILogger<RoomSchedulerOnOff> _logger;


        public RoomSchedulerOnOff(ILogger<RoomSchedulerOnOff> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hàm chính để tạo lịch từ tuần 2 đến tuần 9 với logic ghép phòng.
        /// </summary>
        /// <param name="week1Schedules">Lịch đã hoàn chỉnh của tuần 1.</param>
        /// <param name="allRooms">Danh sách tất cả các phòng học có sẵn.</param>
        /// <param name="allGroupClasses">Danh sách tất cả các lớp học.</param>
        /// <returns>Danh sách lịch hoàn chỉnh cho tuần 2-9.</returns>
        public List<Schedule> GenerateSchedulesForSubsequentWeeks(
            List<Schedule> week1Schedules,
            List<Room> allRooms,
            List<GroupClass> allGroupClasses)
        {
            var subsequentSchedules = new List<Schedule>();
            var availableRooms = new Queue<Room>(allRooms.Where(r => r.Status == "available" && r.TypeOfRoom == "Phòng học"));
            var usedRoomIds = new HashSet<int>();

            // 1. Phân loại các GroupName vào nhóm A và B dựa trên Major
            var groupClassMap = allGroupClasses.ToDictionary(gc => gc.GroupName);
            var schedulesByGroup = week1Schedules.GroupBy(s => s.GroupName).ToList();

            var groupClassesFullOff = schedulesByGroup.Where(g => groupClassMap.ContainsKey(g.Key) && groupClassMap[g.Key].TeachingMode.Trim() == "OFF").ToList();

            foreach (var groupClassFullOff in groupClassesFullOff)
            {
                // BƯỚC 1: GÁN PHÒNG MỘT LẦN CHO CẢ NHÓM LỚP
                // Giả định rằng tất cả các buổi trong cùng 1 nhóm đều có phòng giống nhau từ tuần 1
                var firstSchedule = groupClassFullOff.FirstOrDefault();
                if (firstSchedule == null) continue; // Bỏ qua nếu nhóm rỗng

                var assignedRoom = new Room
                {
                    RoomId = firstSchedule.RoomId.GetValueOrDefault(),
                    RoomName = firstSchedule.RoomName,
                };

                // BƯỚC 2: LOẠI BỎ PHÒNG ĐÃ GÁN KHỎI HÀNG ĐỢI (CHỈ MỘT LẦN)
                if (!usedRoomIds.Contains(assignedRoom.RoomId))
                {
                    usedRoomIds.Add(assignedRoom.RoomId);

                    // Tạo lại hàng đợi mà không chứa phòng đã được gán
                    availableRooms = new Queue<Room>(availableRooms.Where(r => r.RoomId != assignedRoom.RoomId));
                }

                for (int week = 2; week <= 9; week++)
                {
                    foreach (var templateSchedule in groupClassFullOff)
                    {
                        var newSchedule = CreateScheduleForWeek(templateSchedule, week, assignedRoom);
                        newSchedule.StatusSlot = templateSchedule.StatusSlot;
                        subsequentSchedules.Add(newSchedule);
                    }
                }
            }

            var groupA_Classes = new Queue<IGrouping<string, Schedule>>(
                schedulesByGroup.Where(g => groupClassMap.ContainsKey(g.Key) && listMajorGroupA.Contains(groupClassMap[g.Key].Major))
            );

            var groupB_Classes = new Queue<IGrouping<string, Schedule>>(
                schedulesByGroup.Where(g => groupClassMap.ContainsKey(g.Key) && (listMajorGroupB.Contains(groupClassMap[g.Key].Major) || (groupClassMap[g.Key].Major == "GD" && groupClassMap[g.Key].Term == 9)))
            );

            // 2. Ghép cặp các lớp từ nhóm A và B để chia sẻ phòng
            while (groupA_Classes.Any() && groupB_Classes.Any())
            {
                var groupA = groupA_Classes.Dequeue();
                var groupB = groupB_Classes.Dequeue();

                // Tìm phòng phù hợp cho cặp này
                var majorForRoomSelection = groupClassMap[groupA.Key].Major;
                var assignedRoom = FindAndAssignRoom(majorForRoomSelection, availableRooms, usedRoomIds);

                if (assignedRoom == null)
                {
                    _logger.LogWarning($"WARNING: Không còn phòng cho cặp {groupA.Key} và {groupB.Key}.");
                    continue; // Bỏ qua nếu hết phòng
                }

                // 3. Tạo lịch cho cặp này từ tuần 2 đến 9
                for (int week = 2; week <= 9; week++)
                {
                    // Logic luân phiên: Tuần chẵn (2,4,6,8) A học offline thứ lẻ, B học offline thứ chẵn
                    // Tuần lẻ (3,5,7,9) thì ngược lại
                    bool isGroupA_OfflineOnOddDays = (week % 2 == 0);

                    // Tạo lịch cho Group A
                    foreach (var templateSchedule in groupA)
                    {
                        var newSchedule = CreateScheduleForWeek(templateSchedule, week, assignedRoom);
                        var dayOfWeek = (int)templateSchedule.Date.Value.DayOfWeek; // Sunday = 0, Monday = 1
                        bool isOddDay = (dayOfWeek == 1 || dayOfWeek == 3 || dayOfWeek == 5);

                        newSchedule.StatusSlot = (isOddDay == isGroupA_OfflineOnOddDays) ? "OFF" : "ON";
                        subsequentSchedules.Add(newSchedule);
                    }

                    // Tạo lịch cho Group B
                    foreach (var templateSchedule in groupB)
                    {
                        var newSchedule = CreateScheduleForWeek(templateSchedule, week, assignedRoom);
                        var dayOfWeek = (int)templateSchedule.Date.Value.DayOfWeek;
                        bool isOddDay = (dayOfWeek == 1 || dayOfWeek == 3 || dayOfWeek == 5);

                        // Logic của B ngược lại với A
                        newSchedule.StatusSlot = (isOddDay != isGroupA_OfflineOnOddDays) ? "OFF" : "ON";
                        subsequentSchedules.Add(newSchedule);
                    }
                }
            }

            // 4. Xử lý các lớp còn lại không được ghép cặp (nếu có)
            var remainingClasses = groupA_Classes.Concat(groupB_Classes);

            foreach (var remainingGroup in remainingClasses)
            {
                var majorForRoomSelection = groupClassMap[remainingGroup.Key].Major;
                var assignedRoom = FindAndAssignRoom(majorForRoomSelection, availableRooms, usedRoomIds);
                if (assignedRoom == null)
                {
                    _logger.LogWarning($"WARNING: (continue) Không còn phòng cho lớp lẻ {remainingGroup.Key}.");
                    continue;
                }

                for (int week = 2; week <= 9; week++)
                {
                    bool isGroupA_OfflineOnOddDays = (week % 2 == 0);
                    foreach (var templateSchedule in remainingGroup)
                    {
                        var newSchedule = CreateScheduleForWeek(templateSchedule, week, assignedRoom);
                        var dayOfWeek = (int)templateSchedule.Date.Value.DayOfWeek; // Sunday = 0, Monday = 1
                        bool isOddDay = (dayOfWeek == 1 || dayOfWeek == 3 || dayOfWeek == 5);
                        newSchedule.StatusSlot = (isOddDay == isGroupA_OfflineOnOddDays) ? "OFF" : "ON";

                        subsequentSchedules.Add(newSchedule);
                    }
                }
            }

            return subsequentSchedules;
        }

        /// <summary>
        /// Tìm và gán một phòng phù hợp từ danh sách các phòng còn trống.
        /// </summary>
        private Room FindAndAssignRoom(string major, Queue<Room> availableRooms, HashSet<int> usedRoomIds)
        {
            // Ưu tiên tòa nhà "G" cho các ngành "BIT" hoặc "BBA"
            bool requiresGBuilding = major.Contains("BIT") || major.Contains("BBA");

            IEnumerable<Room> potentialRooms = availableRooms;
            if (requiresGBuilding)
            {
                // Thử tìm trong tòa G trước
                var roomInG = availableRooms.FirstOrDefault(r => r.Building == "Gamma" && !usedRoomIds.Contains(r.RoomId));
                if (roomInG != null)
                {
                    usedRoomIds.Add(roomInG.RoomId);
                    // Để đảm bảo phòng được lấy ra khỏi queue, ta cần rebuild queue (cách đơn giản)
                    var updatedQueue = new Queue<Room>(availableRooms.Where(r => r.RoomId != roomInG.RoomId));
                    availableRooms.Clear();
                    while (updatedQueue.Any()) availableRooms.Enqueue(updatedQueue.Dequeue());
                    return roomInG;
                }
            }

            // Nếu không yêu cầu hoặc tòa G đã hết, lấy phòng bất kỳ còn trống
            while (availableRooms.Any())
            {
                var room = availableRooms.Dequeue();
                if (!usedRoomIds.Contains(room.RoomId))
                {
                    usedRoomIds.Add(room.RoomId);
                    return room;
                }
            }

            return null; // Không còn phòng nào
        }

        /// <summary>
        /// Tạo một bản ghi Schedule mới cho một tuần cụ thể dựa trên lịch mẫu.
        /// </summary>
        private Schedule CreateScheduleForWeek(Schedule template, int week, Room room)
        {
            var newSchedule = template.Clone();
            newSchedule.RoomId = room.RoomId;
            newSchedule.RoomName = room.RoomName;
            // Tính ngày mới bằng cách cộng thêm (số tuần - 1) * 7 ngày
            newSchedule.Date = template.Date.Value.AddDays((week - 1) * 7);
            newSchedule.TypeSlot = "NEW SLOT"; // Hoặc logic khác nếu cần
            if (newSchedule.SessionNo == 2)
            {
                newSchedule.SessionNo = 2 * week;
            }
            else
            {
                newSchedule.SessionNo = (2 * week) - 1;
            }
            return newSchedule;
        }
    }
}
