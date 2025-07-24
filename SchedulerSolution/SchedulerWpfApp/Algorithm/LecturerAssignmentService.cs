using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class LecturerAssignmentService
    {
        private Dictionary<string, LecturerAssignmentState> _lecturerStateMap = new();

        // CHANGED: Sửa lỗi chia cho 0
        private void InitializeLecturerStates(List<LecturerSubject> lecturerSubjects)
        {
            _lecturerStateMap = lecturerSubjects
                .GroupBy(ls => ls.LecturerId)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        var state = new LecturerAssignmentState
                        {
                            LecturerId = g.Key,
                            LecturerName = g.First().LecturerName ?? "",
                            LecturerAccount = g.First().Lecturer.LecturerAccount ?? "",
                        };

                        foreach (var ls in g)
                        {
                            if (!string.IsNullOrEmpty(ls.SubjectCode))
                            {
                                state.MaxClassesPerSubject[ls.SubjectCode] = ls.NumberOfClasses ?? 0;

                                // An toàn trước khi chia
                                if (ls.NumberOfClasses.HasValue && ls.NumberOfClasses > 0)
                                {
                                    // Giả sử mỗi lớp cần 2 slot/tuần nếu không có thông tin khác
                                    state.RequiredSlotsPerSubject[ls.SubjectCode] = (ls.TotalSlots / ls.NumberOfClasses) ?? 2;
                                }
                                else
                                {
                                    state.RequiredSlotsPerSubject[ls.SubjectCode] = 2; // Giá trị mặc định an toàn
                                }
                            }
                        }
                        return state;
                    });
        }

        // CHANGED: Thuật toán được viết lại hoàn toàn để đảm bảo tính nhất quán
        public void AssignLecturers(List<LecturerSubject> lecturerSubjects, List<Schedule> allSchedules)
        {
            InitializeLecturerStates(lecturerSubjects);

            var groupedSchedules = allSchedules
                .Where(s => string.IsNullOrEmpty(s.LecturerId) && !string.IsNullOrEmpty(s.SubjectCode) && !string.IsNullOrEmpty(s.GroupName))
                .GroupBy(s => (s.GroupName!, s.SubjectCode!));

            // Lặp qua từng nhóm LỚP-MÔN HỌC, không phải từng slot riêng lẻ
            foreach (var classSubjectGroup in groupedSchedules)
            {
                var groupName = classSubjectGroup.Key.Item1;
                var subjectCode = classSubjectGroup.Key.Item2;
                var schedulesInGroup = classSubjectGroup.ToList();

                // Tìm một giảng viên DUY NHẤT phù hợp cho TẤT CẢ các buổi học của lớp này
                var bestCandidate = FindBestLecturerForEntireClass(subjectCode, schedulesInGroup);

                if (bestCandidate != null)
                {
                    // Nếu tìm thấy, gán giảng viên đó cho tất cả các buổi học
                    foreach (var schedule in schedulesInGroup)
                    {
                        schedule.LecturerId = bestCandidate.LecturerId;
                        schedule.LecturerName = bestCandidate.LecturerName;
                        schedule.LecturerAccount = bestCandidate.LecturerAccount;
                        bestCandidate.Assign(schedule); // Cập nhật trạng thái của giảng viên
                    }
                    Console.WriteLine($"✅ Đã gán GV {bestCandidate.LecturerName} cho lớp {groupName} môn {subjectCode}");
                }
                else
                {
                    Console.WriteLine($"❌ Không tìm được giảng viên nào phù hợp cho TOÀN BỘ các buổi của lớp {groupName} môn {subjectCode}");
                }
            }
        }

        private LecturerAssignmentState? FindBestLecturerForEntireClass(string subjectCode, List<Schedule> schedulesInGroup)
        {
            // 1. Lọc ra những giảng viên có thể dạy môn này
            var potentialLecturers = _lecturerStateMap.Values
                .Where(l => l.MaxClassesPerSubject.ContainsKey(subjectCode));

            // 2. Tìm ứng viên thỏa mãn TẤT CẢ các ràng buộc cho TẤT CẢ các lịch học trong nhóm
            var validCandidates = potentialLecturers
                .Where(lecturer =>
                    schedulesInGroup.All(schedule => // Phải thỏa mãn TẤT CẢ (All) các lịch
                        lecturer.IsAvailable(schedule) &&
                        lecturer.CanTeachThisClassSubject(schedule) &&
                        lecturer.IsValidDayOfWeekForTwoClasses(schedule)
                    )
                )
                .OrderBy(l => l.GetAssignedGroupCount(subjectCode)) // Ưu tiên người dạy ít lớp môn này nhất
                .ThenBy(l => l.TotalAssignedGroups) // Nếu bằng nhau thì ưu tiên người có tổng số lớp ít hơn
                .ToList();

            return validCandidates.FirstOrDefault();
        }
    }
}
