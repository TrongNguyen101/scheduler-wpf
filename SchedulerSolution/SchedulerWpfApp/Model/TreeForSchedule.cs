namespace SchedulerWpfApp.Model
{
    public class TreeForSchedule
    {
        public string Value { get; set; }
        public TreeForSchedule Left { get; set; }
        public TreeForSchedule Right { get; set; }
        public string? RoomNo { get; set; }
        public string? PartOfDay { get; set; }
        public string? SlotTime { get; set; }
        public string? StatusSlot { get; set; }

        public TreeForSchedule() { }
        public TreeForSchedule(string value, string roomNo = null, string partOfDay = null, string slotTime = null, string statusSlot = null)
        {
            Value = value;
            RoomNo = roomNo;
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
        public TreeForSchedule BuildTreeForRoom(string room)
        {
            // First level: Room
            // Create the root node with the room name
            TreeForSchedule root = new TreeForSchedule(room, roomNo: room);

            // Second level: PartOfDay (AM, PM)
            root.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "AM");
            root.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "PM");

            // Third level: Slot (Slot 1, Slot 2, Slot 3, Slot 4)
            root.Left.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 1");
            root.Left.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 2");
            root.Right.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 3");
            root.Right.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 4");

            // Fourth level: Slot Type (Online, Offline)
            // Each slot has two types: Online and Offline
            root.Left.Left.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 1", statusSlot: "online");
            root.Left.Left.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 1", statusSlot: "offline");
            root.Left.Right.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 2", statusSlot: "online");
            root.Left.Right.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "AM", slotTime: "slot 2", statusSlot: "offline");
            root.Right.Left.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 3", statusSlot: "online");
            root.Right.Left.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 3", statusSlot: "offline");
            root.Right.Right.Left = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 4", statusSlot: "online");
            root.Right.Right.Right = new TreeForSchedule(room, roomNo: room, partOfDay: "PM", slotTime: "slot 4", statusSlot: "offline");

            return root;
        }

        // Thu thập lịch trình từ các node lá cho một ngày cụ thể
        public void CollectSchedules(TreeForSchedule node, List<Schedule> schedules, string subject, DateTime date, string classId, string slotTime, string lecturerName, string slotTypeCode, string typeSlot, string sessionNo, string partOfDayFilter = null, string statusSlot = null)
        {
            // Nếu node là null, trả về
            // Nếu có bộ lọc phiên (AM hoặc PM), chỉ xử lý các node thuộc nhánh phù hợp
            // Nếu là node lá (có SlotType, tức cấp 4), tạo mục lịch trình
            // Duyệt đệ quy các nhánh trái và phải
            {
                if (node == null) return;

                // Nếu có bộ lọc phiên (AM hoặc PM), chỉ xử lý các node thuộc nhánh phù hợp
                if (partOfDayFilter != null && node.PartOfDay != null && node.PartOfDay != partOfDayFilter)
                {
                    return; // Bỏ qua node nếu không thuộc nhánh được yêu cầu
                }

                if (statusSlot != null && node.StatusSlot != null && node.StatusSlot != statusSlot)
                {
                    return; // Bỏ qua node nếu không thuộc nhánh được yêu cầu
                }

                if (slotTime != null && node.SlotTime != null && node.SlotTime != slotTime)
                {
                    return; // Bỏ qua node nếu không thuộc nhánh được yêu cầu
                }

                // Nếu là node lá (có SlotType, tức cấp 4), tạo mục lịch trình
                if (node.Left == null && node.Right == null && node.StatusSlot != null)
                {
                    Schedule schedule = new Schedule(node.RoomNo, node.PartOfDay, node.SlotTime, node.StatusSlot, subject, date, classId, lecturerName, slotTypeCode, typeSlot, sessionNo);
                    schedules.Add(schedule);
                }

                // Duyệt đệ quy các nhánh trái và phải
                CollectSchedules(node.Left, schedules, subject, date, classId, slotTime, lecturerName, slotTypeCode, typeSlot, sessionNo, partOfDayFilter, statusSlot);
                CollectSchedules(node.Right, schedules, subject, date, classId, slotTime, lecturerName, slotTypeCode, typeSlot, sessionNo, partOfDayFilter, statusSlot);
            }
        }
    }
}
