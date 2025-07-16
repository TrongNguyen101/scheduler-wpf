using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Algorithm
{
    // Lớp thực hiện nhiệm vụ phân công giảng viên vào các lịch học đã xếp sẵn.
    // Sử dụng chiến lược Greedy có kiểm tra xung đột theo các ràng buộc về chuyên môn, thời gian và số lớp.
    // Bao gồm: kiểm tra số lớp tối đa mỗi môn, slot rảnh trong tuần, không dạy nhiều môn cho cùng lớp,
    // và nếu tổng lớp là 2 thì chỉ xếp vào cặp ngày cố định (T2-T4 hoặc T3-T5).
    public class LecturerAssignmentService
    {
        private readonly List<LecturerSubject> _lecturerSubjects;
        private readonly List<Schedule> _schedules;
        private Dictionary<string, LecturerAssignmentState> _lecturerStateMap = new();

        public LecturerAssignmentService(List<LecturerSubject> lecturerSubjects, List<Schedule> schedules)
        {
            _lecturerSubjects = lecturerSubjects;
            _schedules = schedules;
            InitializeLecturerStates();
        }

        private void InitializeLecturerStates()
        {
            _lecturerStateMap = _lecturerSubjects
                .GroupBy(ls => ls.LecturerId)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        var state = new LecturerAssignmentState
                        {
                            LecturerId = g.Key,
                            LecturerName = g.First().LecturerName ?? "",
                            LecturerAccount = g.First().Lecturer.LecturerAccount ?? ""
                        };

                        foreach (var ls in g)
                        {
                            if (!string.IsNullOrEmpty(ls.SubjectCode))
                            {
                                state.MaxClassesPerSubject[ls.SubjectCode] = ls.NumberOfClasses ?? 0;
                                state.RequiredSlotsPerSubject[ls.SubjectCode] = (ls.TotalSlots / ls.NumberOfClasses) ?? 0;
                            }
                        }

                        return state;
                    });
        }

        public void AssignLecturers()
        {
            var groupedSchedules = _schedules
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
                    // Bổ sung kiểm tra: lọc ra những giảng viên đã được phân ít slot hơn số lượng yêu cầu cho môn học
                    schedules = schedules.OrderBy(s => s.Date).ToList();

                    {
                        // Lọc ra danh sách giảng viên phù hợp với lịch này, theo các ràng buộc về môn học, thời gian, lớp
                        var candidates = _lecturerStateMap.Values
                            .Where(l =>
                                l.MaxClassesPerSubject.ContainsKey(subjectCode) // Giảng viên có được dạy môn này không
                                && l.IsAvailable(schedule) // Giảng viên có rảnh không
                                && l.CanTeachThisClassSubject(schedule) // Có được dạy lớp này không (chỉ dạy 1 môn/lớp)
                                && l.IsValidDayOfWeekForTwoClasses(schedule) // Nếu chỉ dạy 2 lớp thì phải đúng cặp ngày
                                && l.RequiredSlotsPerSubject[subjectCode] > l.UsedSlots.Count(slot => slot.date.Date == schedule.Date!.Value.Date && slot.partOfDay == schedule.PartOfDay && slot.slotTime == schedule.SlotTime) // Chưa dạy đủ số slot của môn
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
