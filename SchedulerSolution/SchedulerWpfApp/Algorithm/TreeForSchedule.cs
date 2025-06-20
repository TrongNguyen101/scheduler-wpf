using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.RoomService;

namespace SchedulerWpfApp.Algorithm
{
    public class TreeForSchedule
    {
        private readonly IRoomService _roomService;

        public string RoomName { get; set; }
        public TreeForSchedule Left { get; set; }
        public TreeForSchedule Right { get; set; }
        public string? PartOfDay { get; set; }
        public string? SlotTime { get; set; }
        public string? StatusSlot { get; set; }

        public TreeForSchedule(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public TreeForSchedule(string value, string partOfDay = null, string slotTime = null, string statusSlot = null)
        {
            RoomName = value;
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

        public async Task<TreeForSchedule> BuildTreeForRoom(string roomName, string typeOfSlot)
        {
            // First level: Room
            // Create the root node with the room name
            TreeForSchedule root = new TreeForSchedule(roomName);

            // Second level: PartOfDay (AM, PM)
            root.Left = new TreeForSchedule(roomName, partOfDay: "A");
            root.Right = new TreeForSchedule(roomName, partOfDay: "P");

            // Third level: Slot (Slot 1, Slot 2, Slot 3, Slot 4)
            root.Left.Left = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 1");
            root.Left.Right = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 2");
            root.Right.Left = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 3");
            root.Right.Right = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 4");

            // Fourth level: Slot Type (Online, Offline)
            // Each slot has two types: Online and Offline
            root.Left.Left.Left = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 1", statusSlot: "online");
            root.Left.Left.Right = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 1", statusSlot: "offline");
            root.Left.Right.Left = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 2", statusSlot: "online");
            root.Left.Right.Right = new TreeForSchedule(roomName, partOfDay: "A", slotTime: "slot 2", statusSlot: "offline");
            root.Right.Left.Left = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 3", statusSlot: "online");
            root.Right.Left.Right = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 3", statusSlot: "offline");
            root.Right.Right.Left = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 4", statusSlot: "online");
            root.Right.Right.Right = new TreeForSchedule(roomName, partOfDay: "P", slotTime: "slot 4", statusSlot: "offline");

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
                    RoomName = node.RoomName,
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
