using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.Services;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Algorithm
{
    public class TreeForSchedule
    {
        private readonly IRoomServiceOld _roomService;

        public string Value { get; set; }
        public TreeForSchedule Left { get; set; }
        public TreeForSchedule Right { get; set; }
        public int? RoomId { get; set; }
        public string? PartOfDay { get; set; }
        public string? SlotTime { get; set; }
        public string? StatusSlot { get; set; }

        public TreeForSchedule(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public TreeForSchedule(string value, int? roomId = null, string partOfDay = null, string slotTime = null, string statusSlot = null)
        {
            Value = value;
            RoomId = roomId;
            PartOfDay = partOfDay;
            SlotTime = slotTime;
            StatusSlot = statusSlot;
            Left = null;
            Right = null;
        }

        // Build a binary tree for a given room
        // The tree structure is as follows:
        // - Room (root)
        //   - PartOfDay (AM, PM)
        //     - Slot (Slot 1, Slot 2, Slot 3, Slot 4)
        //       - Slot Type (Online, Offline)

        public async Task<TreeForSchedule> BuildTreeForRoom(int? roomId, string typeOfSlot)
        {
            if (roomId is null)
                throw new ArgumentNullException(nameof(roomId), "Room ID cannot be null.");

            Room? room = await _roomService.GetByIdAsync(roomId.Value);
            if (room is null)
                throw new InvalidOperationException($"Room with ID {roomId} not found.");

            // First level: Room
            // Create the root node with the room name
            TreeForSchedule root = new TreeForSchedule(room.RoomName, roomId);

            // Second level: PartOfDay (AM, PM)
            root.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A");
            root.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P");

            // Third level: Slot (Slot 1, Slot 2, Slot 3, Slot 4)
            root.Left.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 1");
            root.Left.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 2");
            root.Right.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 3");
            root.Right.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 4");

            // Fourth level: Slot Type (Online, Offline)
            // Each slot has two types: Online and Offline
            root.Left.Left.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 1", statusSlot: "online");
            root.Left.Left.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 1", statusSlot: "offline");
            root.Left.Right.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 2", statusSlot: "online");
            root.Left.Right.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "A", slotTime: "slot 2", statusSlot: "offline");
            root.Right.Left.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 3", statusSlot: "online");
            root.Right.Left.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 3", statusSlot: "offline");
            root.Right.Right.Left = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 4", statusSlot: "online");
            root.Right.Right.Right = new TreeForSchedule(room.RoomName, roomId, partOfDay: "P", slotTime: "slot 4", statusSlot: "offline");

            return root;
        }

        public List<Schedule> CollectSchedules(TreeForSchedule node,
                                                string subject,
                                                DateTime date,
                                                string classId,
                                                string slotTime,
                                                string lecturerName,
                                                string slotTypeCode,
                                                string typeSlot,
                                                int sessionNo,
                                                string partOfDayFilter = null,
                                                string statusSlot = null)
        {
            var schedules = new List<Schedule>();

            if (node == null)
                return schedules;

            // Bộ lọc buổi học
            if (partOfDayFilter != null && node.PartOfDay != null && node.PartOfDay != partOfDayFilter)
                return schedules;

            // Bộ lọc trạng thái slot
            if (statusSlot != null && node.StatusSlot != null && node.StatusSlot != statusSlot)
                return schedules;

            // Bộ lọc theo slot time
            if (slotTime != null && node.SlotTime != null && node.SlotTime != slotTime)
                return schedules;

            // Nếu là node lá, tạo Schedule
            if (node.Left == null && node.Right == null && node.StatusSlot != null)
            {
                schedules.Add(new Schedule
                {
                    RoomId = node.RoomId,
                    PartOfDay = node.PartOfDay,
                    SlotTime = node.SlotTime,
                    StatusSlot = node.StatusSlot,
                    SubjectCode = subject,
                    Date = date,
                    GroupName = classId,
                    LecturerId = lecturerName,
                    SlotTypeCode = slotTypeCode,
                    TypeSlot = typeSlot,
                    SessionNo = sessionNo
                });
            }

            // Đệ quy các nhánh con và gộp kết quả
            schedules.AddRange(CollectSchedules(node.Left, subject, date, classId,
                slotTime, lecturerName, slotTypeCode, typeSlot, sessionNo,
                partOfDayFilter, statusSlot));

            schedules.AddRange(CollectSchedules(node.Right, subject, date, classId,
                slotTime, lecturerName, slotTypeCode, typeSlot, sessionNo,
                partOfDayFilter, statusSlot));

            return schedules;
        }

    }
}
