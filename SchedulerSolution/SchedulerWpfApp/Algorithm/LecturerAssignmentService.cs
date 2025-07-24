using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    // Lớp thực hiện nhiệm vụ phân công giảng viên vào các lịch học đã xếp sẵn.
    // Sử dụng chiến lược Greedy có kiểm tra xung đột theo các ràng buộc về chuyên môn, thời gian và số lớp.
    // Bao gồm: kiểm tra số lớp tối đa mỗi môn, slot rảnh trong tuần, không dạy nhiều môn cho cùng lớp,
    // và nếu tổng lớp là 2 thì chỉ xếp vào cặp ngày cố định (T2-T4 hoặc T3-T5).
    public class LecturerAssignmentService
    {
        private Dictionary<string, LecturerAssignmentState> _lecturerStateMap = new();

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
                                state.TotalAssignedGroupsAllSubjects += ls.NumberOfClasses ?? 0;
                                state.MaxClassesPerSubject[ls.SubjectCode] = ls.NumberOfClasses ?? 0;
                                state.RequiredSlotsPerSubject[ls.SubjectCode] = (ls.TotalSlots / ls.NumberOfClasses) ?? 0;
                            }
                        }

                        return state;
                    });
        }

        public void AssignLecturers(List<LecturerSubject> lecturerSubjects, List<Schedule> allSchedules)
        {
            InitializeLecturerStates(lecturerSubjects);
            var listLecturerHaveTwoClasses = _lecturerStateMap.Values
                .Where(l => l.TotalAssignedGroupsAllSubjects == 2)
                .ToList();
            var listLecturerHave4Classes = _lecturerStateMap.Values
                .Where(l => l.TotalAssignedGroupsAllSubjects == 4)
                .ToList();

            var groupedSchedules = allSchedules
                .Where(s => string.IsNullOrEmpty(s.LecturerId) && !string.IsNullOrEmpty(s.SubjectCode) && !string.IsNullOrEmpty(s.GroupName))
                .GroupBy(s => (s.GroupName!, s.SubjectCode!));

            foreach (var classSubjectGroup in groupedSchedules)
            {
                var groupName = classSubjectGroup.Key.Item1;
                var subjectCode = classSubjectGroup.Key.Item2;
                var schedules = classSubjectGroup.ToList();

                // Duyệt qua từng lịch học trong nhóm lớp-môn để gán giảng viên
                foreach (var schedule in schedules)
                {
                    {
                        // Lọc ra danh sách giảng viên phù hợp với lịch này, theo các ràng buộc về môn học, thời gian, lớp
                        var candidates = _lecturerStateMap.Values
                         .Where(l =>
                             l.MaxClassesPerSubject.ContainsKey(subjectCode) // Giảng viên có được dạy môn này không
                             && l.IsAvailable(schedule) // Giảng viên có rảnh không
                             && l.CanTeachThisClassSubject(schedule) // Có được dạy lớp này không (chỉ dạy 1 môn/lớp)
                             && l.IsValidDayOfWeekForTwoClasses(schedule) // Nếu chỉ dạy 2 lớp thì phải đúng cặp ngày
                         )
                         .OrderBy(l => l.GetAssignedGroupCount(subjectCode)) // Ưu tiên giảng viên dạy ít lớp hơn
                         .ToList();

                        if (candidates.Any())
                        {
                            var selected = candidates.First();
                            schedule.LecturerId = selected.LecturerId;
                            schedule.LecturerName = selected.LecturerName;
                            schedule.LecturerAccount = selected.LecturerAccount;
                            selected.Assign(schedule);
                        }
                        else
                        {
                            Console.WriteLine($"❌ Không tìm được giảng viên phù hợp cho lớp {groupName} môn {subjectCode} ngày {schedule.Date:ddd dd/MM}");
                        }
                    }
                }
            }
        }
    }
}
