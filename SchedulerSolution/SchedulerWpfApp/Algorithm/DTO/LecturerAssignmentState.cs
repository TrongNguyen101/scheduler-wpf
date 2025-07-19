using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm.DTO
{
    public class LecturerAssignmentState
    {
        public string LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string LecturerAccount { get; set; } // Tài khoản giảng viên

        // Tổng số lớp giảng viên đã dạy ở tất cả các môn
        public int TotalAssignedGroupsAllSubjects { get; set; } = 0;

        // Số lớp tối đa mà giảng viên được dạy cho từng môn học
        public Dictionary<string, int> MaxClassesPerSubject { get; set; } = new();

        // Tổng số slot phải dạy cho từng môn học (theo tuần)
        public Dictionary<string, int> RequiredSlotsPerSubject { get; set; } = new();

        // Danh sách các lớp mà giảng viên đã được gán cho từng môn
        public Dictionary<string, HashSet<string>> AssignedGroupsPerSubject { get; set; } = new();

        // Các slot thời gian mà giảng viên đã bị chiếm
        public HashSet<(DateTime date, string partOfDay, int slotTime)> UsedSlots = new();

        // Số lượng lớp đã dạy trong mỗi buổi (AM hoặc PM)
        public Dictionary<(DateTime date, string partOfDay), int> DailySessionLoads = new();

        // Lưu lại môn mà giảng viên đã dạy cho mỗi lớp để tránh dạy nhiều môn trong cùng một lớp
        public Dictionary<string, string> ClassToSubjectTaught = new(); // [GroupName] = SubjectCode

        // Kiểm tra giảng viên có rảnh tại slot được yêu cầu không, đồng thời chưa dạy quá 2 lớp trong buổi đó
        public bool IsAvailable(Schedule schedule)
        {
            var key = (schedule.Date!.Value, schedule.PartOfDay!, schedule.SlotTime!.Value);
            var sessionKey = (schedule.Date.Value, schedule.PartOfDay!);
            return !UsedSlots.Contains(key) && (!DailySessionLoads.TryGetValue(sessionKey, out int count) || count < 2);
        }

        // Kiểm tra xem giảng viên có được dạy môn này ở lớp này không, đảm bảo không dạy nhiều môn trong 1 lớp
        public bool CanTeachThisClassSubject(Schedule s)
        {
            // Kiểm tra xem lớp này đã có giảng viên dạy môn này chưa
            if (ClassToSubjectTaught.TryGetValue(s.GroupName!, out var subjectAlreadyTaught))
                return subjectAlreadyTaught == s.SubjectCode;

            if (ClassToSubjectTaught.ContainsKey(s.GroupName!)) return false; // Đã dạy môn khác trong lớp này rồi

            if (!MaxClassesPerSubject.ContainsKey(s.SubjectCode!)) return false;

            if (!AssignedGroupsPerSubject.ContainsKey(s.SubjectCode!))
                AssignedGroupsPerSubject[s.SubjectCode!] = new();

            return AssignedGroupsPerSubject[s.SubjectCode!].Count < MaxClassesPerSubject[s.SubjectCode!]
                || AssignedGroupsPerSubject[s.SubjectCode!].Contains(s.GroupName!);
        }

        // Kiểm tra nếu tổng số lớp là 2 thì chỉ được xếp vào thứ 2&4 hoặc thứ 3&5
        public bool IsValidDayOfWeekForTwoClasses(Schedule s)
        {
            if (TotalAssignedGroupsAllSubjects != 2) return true;
            var day = s.Date!.Value.DayOfWeek;
            return day == DayOfWeek.Monday || day == DayOfWeek.Wednesday ||
                   day == DayOfWeek.Tuesday || day == DayOfWeek.Thursday;
        }

        // Gán giảng viên vào một slot và cập nhật tất cả trạng thái liên quan
        public void Assign(Schedule s)
        {
            UsedSlots.Add((s.Date!.Value, s.PartOfDay!, s.SlotTime!.Value));

            var sessionKey = (s.Date.Value, s.PartOfDay!);
            if (!DailySessionLoads.ContainsKey(sessionKey))
                DailySessionLoads[sessionKey] = 0;
            DailySessionLoads[sessionKey]++;

            if (!AssignedGroupsPerSubject.ContainsKey(s.SubjectCode!))
                AssignedGroupsPerSubject[s.SubjectCode!] = new();
            AssignedGroupsPerSubject[s.SubjectCode!].Add(s.GroupName!);

            ClassToSubjectTaught[s.GroupName!] = s.SubjectCode!;
        }

        // Đếm số lớp đã được phân công cho môn học cụ thể
        public int GetAssignedGroupCount(string subject)
        {
            return AssignedGroupsPerSubject.GetValueOrDefault(subject)?.Count ?? 0;
        }

        // Đếm tổng số slot đã gán cho môn học (để đảm bảo đủ 2 slot/tuần/lớp)
        public int GetAssignedSlotCount(string subject)
        {
            return UsedSlots.Count(s => AssignedGroupsPerSubject.GetValueOrDefault(subject)?.Contains(s.date.ToString()) == true);
        }
    }



}
