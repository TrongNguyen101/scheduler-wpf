using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.RoomService;

namespace SchedulerWpfApp.Algorithm
{
    public class TreeForSchedule
    {
        private readonly IRoomService _roomService;

        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public TreeForSchedule Left { get; set; }
        public TreeForSchedule Right { get; set; }
        public string? PartOfDay { get; set; }
        public int? SlotTime { get; set; }
        public string? StatusSlot { get; set; }

        public TreeForSchedule(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public TreeForSchedule(int roomId, string value, string partOfDay = null, int? slotTime = null, string statusSlot = null)
        {
            RoomId = roomId;
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

        public async Task<TreeForSchedule> BuildTreeForRoom(int roomId, string roomName)
        {
            // First level: Room
            // Create the root node with the room name
            TreeForSchedule root = new TreeForSchedule(roomId, roomName);

            // Second level: PartOfDay (AM, PM)
            root.Left = new TreeForSchedule(roomId, roomName, partOfDay: "A");
            root.Right = new TreeForSchedule(roomId, roomName, partOfDay: "P");

            // Third level: Slot (Slot 1, Slot 2, Slot 3, Slot 4)
            root.Left.Left = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 1);
            root.Left.Right = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 2);
            root.Right.Left = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 3);
            root.Right.Right = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 4);

            // Fourth level: Slot Type (Online, Offline)
            // Each slot has two types: Online and Offline
            root.Left.Left.Left = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 1, statusSlot: ScheduleConstants.StatusSlotIsOnline);
            root.Left.Left.Right = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 1, statusSlot: ScheduleConstants.StatusSlotIsOffline);
            root.Left.Right.Left = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 2, statusSlot: ScheduleConstants.StatusSlotIsOnline);
            root.Left.Right.Right = new TreeForSchedule(roomId, roomName, partOfDay: "A", slotTime: 2, statusSlot: ScheduleConstants.StatusSlotIsOffline);
            root.Right.Left.Left = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 3, statusSlot: ScheduleConstants.StatusSlotIsOnline);
            root.Right.Left.Right = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 3, statusSlot: ScheduleConstants.StatusSlotIsOffline);
            root.Right.Right.Left = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 4, statusSlot: ScheduleConstants.StatusSlotIsOnline);
            root.Right.Right.Right = new TreeForSchedule(roomId, roomName, partOfDay: "P", slotTime: 4, statusSlot: ScheduleConstants.StatusSlotIsOffline);

            return root;
        }

        public List<Schedule> CollectSchedules(TreeForSchedule node,
                                                string subjectCode,
                                                DateTime date,
                                                string groupName,
                                                int slotTime,
                                                string lecturerId,
                                                string lecturerName,
                                                string lecturerAccount,
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
                string roomName = node.RoomName;
                if (node.StatusSlot == ScheduleConstants.StatusSlotIsOnline)
                {
                    roomName = node.RoomName + "ON";
                } else if (typeSlot == ScheduleConstants.TypeSlotIsNew)
                {
                    roomName = "R." + node.RoomName;
                }

                    schedules.Add(new Schedule
                    {
                        RoomId = node.RoomId,
                        RoomName = roomName,
                        PartOfDay = node.PartOfDay,
                        SlotTime = node.SlotTime,
                        StatusSlot = node.StatusSlot,
                        SubjectCode = subjectCode,
                        Date = date,
                        GroupName = groupName,
                        LecturerId = lecturerId,
                        LecturerName = lecturerName,
                        LecturerAccount = lecturerAccount,
                        SlotTypeCode = slotTypeCode,
                        TypeSlot = typeSlot,
                        SessionNo = sessionNo,
                    });
            }

            // Đệ quy các nhánh con và gộp kết quả
            schedules.AddRange(CollectSchedules(node.Left, subjectCode, date, groupName,
                slotTime, lecturerId, lecturerName, lecturerAccount, slotTypeCode, typeSlot, sessionNo,
                partOfDayFilter, statusSlot));

            schedules.AddRange(CollectSchedules(node.Right, subjectCode, date, groupName,
                slotTime, lecturerId, lecturerName, lecturerAccount, slotTypeCode, typeSlot, sessionNo,
                partOfDayFilter, statusSlot));

            return schedules;
        }

    }
}
