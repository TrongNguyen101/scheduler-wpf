using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm.DTO
{
    /// <summary>
    /// Lớp LecturerAssignmentState sau khi đã được tối ưu hóa.
    /// - Loại bỏ các thuộc tính dư thừa: DailySessionLoads, TotalAssignedGroupsAllSubjects.
    /// - Sửa đổi cấu trúc của UsedSlots để lưu trữ toàn bộ đối tượng Schedule, giúp truy vấn dễ dàng và sửa lỗi logic.
    /// - Các phương thức được cập nhật để hoạt động với cấu trúc dữ liệu mới, đảm bảo tính nhất quán và đơn giản hóa logic.
    /// </summary>
    public class LecturerAssignmentState
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string LecturerAccount { get; set; }

        // Số lớp tối đa mà giảng viên được dạy cho từng môn học
        public Dictionary<string, int> MaxClassesPerSubject { get; set; } = new();

        // Tổng số slot phải dạy cho từng môn học (theo tuần)
        public Dictionary<string, int> RequiredSlotsPerSubject { get; set; } = new();

        // Danh sách các lớp mà giảng viên đã được gán cho từng môn
        public Dictionary<string, HashSet<string>> AssignedGroupsPerSubject { get; set; } = new();

        // CHANGED: Lưu toàn bộ đối tượng Schedule để có đầy đủ thông tin (SubjectCode, GroupName, etc.)
        // Điều này sửa lỗi logic trong GetAssignedSlotCount và làm cho các truy vấn khác mạnh mẽ hơn.
        public HashSet<Schedule> UsedSlots { get; private set; } = new();

        // Lưu lại môn mà giảng viên đã dạy cho mỗi lớp để tránh dạy nhiều môn trong cùng một lớp
        public Dictionary<string, string> ClassToSubjectTaught { get; private set; } = new(); // [GroupName] = SubjectCode

        // NEW: Thuộc tính được tính toán để lấy tổng số lớp đã gán.
        // Điều này đảm bảo dữ liệu luôn nhất quán và loại bỏ việc cập nhật thủ công.
        public int TotalAssignedGroups => AssignedGroupsPerSubject.Values.SelectMany(set => set).Distinct().Count();

        /// <summary>
        /// Kiểm tra giảng viên có rảnh tại slot được yêu cầu không, đồng thời chưa dạy quá 2 lớp trong buổi đó.
        /// </summary>
        // CHANGED: Logic được cập nhật để hoạt động với HashSet<Schedule> và tính toán tải của buổi học (session load) một cách linh hoạt.
        public bool IsAvailable(Schedule schedule)
        {
            // 1. Kiểm tra xem có slot nào trùng khớp chính xác về thời gian không.
            if (UsedSlots.Any(s => s.Date!.Value.Date == schedule.Date!.Value.Date &&
                                   s.PartOfDay == schedule.PartOfDay &&
                                   s.SlotTime == schedule.SlotTime))
            {
                return false; // Slot thời gian này đã bị chiếm.
            }

            // 2. Tính toán số lớp trong buổi (sáng/chiều) để đảm bảo không quá 2.
            int sessionLoad = UsedSlots.Count(s =>
                s.Date!.Value.Date == schedule.Date!.Value.Date &&
                s.PartOfDay == schedule.PartOfDay);

            return sessionLoad < 2;
        }

        /// <summary>
        /// Kiểm tra xem giảng viên có thể dạy môn này ở lớp này không, dựa trên các ràng buộc.
        /// </summary>
        public bool CanTeachThisClassSubject(Schedule s)
        {
            // 1. Nếu giảng viên đã dạy một môn khác trong lớp này rồi -> không thể.
            if (ClassToSubjectTaught.TryGetValue(s.GroupName!, out var subjectAlreadyTaught) && subjectAlreadyTaught != s.SubjectCode)
            {
                return false;
            }

            // 2. Kiểm tra xem giảng viên có được đăng ký dạy môn này không.
            if (!MaxClassesPerSubject.TryGetValue(s.SubjectCode!, out int maxClasses))
            {
                return false; // Giảng viên không được phân công dạy môn này.
            }

            var assignedGroupsForSubject = AssignedGroupsPerSubject.GetValueOrDefault(s.SubjectCode!, new HashSet<string>());

            // 3. Giảng viên có thể dạy nếu:
            //    a. Số lớp họ dạy cho môn này chưa đạt tối đa.
            //    b. Hoặc, đây chính là lớp họ đã được gán (trường hợp xếp các slot tiếp theo cho cùng một lớp).
            return assignedGroupsForSubject.Count < maxClasses || assignedGroupsForSubject.Contains(s.GroupName!);
        }

        /// <summary>
        /// Kiểm tra nếu tổng số lớp là 2 thì chỉ được xếp vào thứ 2&4 hoặc thứ 3&5.
        /// </summary>
        // CHANGED: Sử dụng thuộc tính tính toán `TotalAssignedGroups` để đảm bảo tính đúng đắn.
        public bool IsValidDayOfWeekForTwoClasses(Schedule s)
        {
            if (this.TotalAssignedGroups != 2) return true;

            var day = s.Date!.Value.DayOfWeek;
            return day == DayOfWeek.Monday || day == DayOfWeek.Wednesday ||
                   day == DayOfWeek.Tuesday || day == DayOfWeek.Thursday;
        }

        /// <summary>
        /// Gán giảng viên vào một Schedule và cập nhật tất cả trạng thái liên quan.
        /// </summary>
        // CHANGED: Logic được đơn giản hóa, không cần cập nhật các thuộc tính đã bị loại bỏ.
        public void Assign(Schedule s)
        {
            // Chỉ cần thêm đối tượng Schedule vào tập hợp.
            UsedSlots.Add(s);

            // Cập nhật các lớp đã gán cho môn học.
            if (!AssignedGroupsPerSubject.ContainsKey(s.SubjectCode!))
            {
                AssignedGroupsPerSubject[s.SubjectCode!] = new HashSet<string>();
            }
            AssignedGroupsPerSubject[s.SubjectCode!].Add(s.GroupName!);

            // Đánh dấu lớp này đã được dạy môn này bởi giảng viên này.
            ClassToSubjectTaught[s.GroupName!] = s.SubjectCode!;
        }

        /// <summary>
        /// Đếm số lớp đã được phân công cho môn học cụ thể.
        /// </summary>
        public int GetAssignedGroupCount(string subjectCode)
        {
            return AssignedGroupsPerSubject.GetValueOrDefault(subjectCode, new HashSet<string>()).Count;
        }

        /// <summary>
        /// Đếm tổng số slot đã gán cho môn học (để đảm bảo đủ 2 slot/tuần/lớp).
        /// </summary>
        // CHANGED: Logic được sửa lại hoàn toàn và giờ đã chính xác nhờ `UsedSlots` chứa `Schedule`.
        public int GetAssignedSlotCount(string subjectCode)
        {
            return UsedSlots.Count(s => s.SubjectCode == subjectCode);
        }
    }
}
