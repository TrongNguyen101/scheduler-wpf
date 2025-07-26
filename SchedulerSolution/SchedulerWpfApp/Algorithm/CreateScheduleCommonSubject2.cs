using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using System.Linq;
namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleCommonSubject2
    {
        private readonly ILogger<CreateScheduleCommonSubject2> _logger;

        public CreateScheduleCommonSubject2(ILogger<CreateScheduleCommonSubject2> logger)
        {
            _logger = logger;
        }

        // Các model nội bộ để giúp xử lý dễ dàng hơn
        private record TeachingRequirement(GroupClass GroupClass, CurriculumSubject Subject);
        private record Assignment(GroupClass GroupClass, CurriculumSubject Subject, string LecturerId, string GroupName, string SubjectCode);


        /// <summary>
        /// Phương thức chính để tạo ra một thời khóa biểu hoàn chỉnh.
        /// </summary>
        public List<Schedule> CreateSchedule(
            List<GroupClass> allGroupClasses,
            List<LecturerSubject> lecturerSubjects,
            ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup)
        {
            // === GIAI ĐOẠN 1: PHÂN CÔNG GIẢNG VIÊN ===
            var assignments = AssignLecturersToRequirements(allGroupClasses, lecturerSubjects, curriculumLookup);
            if (!assignments.Any())
            {
                _logger.LogError("Giai đoạn 1 thất bại: Không có phân công giảng viên nào được tạo ra.");
                return new List<Schedule>();
            }

            // === GIAI ĐOẠN 2: XẾP LỊCH VÀO THỜI KHÓA BIỂU ===
            var finalSchedules = PlaceAssignmentsIntoTimetable(assignments);

            _logger.LogInformation($"Hoàn tất xếp lịch: {finalSchedules.Count}/{assignments.Count} phân công đã được xếp thành công.");

            return finalSchedules;
        }


        #region Giai đoạn 1: Phân công Giảng viên

        private List<Assignment> AssignLecturersToRequirements(
            List<GroupClass> allGroupClasses,
            List<LecturerSubject> lecturerSubjects,
            ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup)
        {
            // 1a. Tạo danh sách tất cả các yêu cầu giảng dạy (Demand)
            var allRequirements = new List<TeachingRequirement>();
            foreach (var groupName in allGroupClasses)
            {
                var subjectsOfClass = curriculumLookup[(groupName.CurriculumCode, groupName.Term.GetValueOrDefault())].ToList();
                var listSubjectOnOffNomalAndHalfOne = subjectsOfClass
                                                    .Where(s => s.TeachingMode == ScheduleConstants.TechingModeIsOnOff && (s.PartOfTerm.Contains("H1") || s.PartOfTerm == "All") && !s.SubjectCode.Contains("GRA"))
                                                    .ToList();
                foreach (var subject in listSubjectOnOffNomalAndHalfOne)
                {
                    allRequirements.Add(new TeachingRequirement(groupName, subject));
                }
            }

            // 1b. Tạo bản đồ năng lực của giảng viên (Supply)
            var lecturerCapacity = lecturerSubjects.ToDictionary(
                ls => (ls.Lecturer.LecturerId, ls.SubjectCode),
                ls => ls.NumberOfClasses.GetValueOrDefault()
            );

            // 1c. Bắt đầu gán
            var successfulAssignments = new List<Assignment>();
            var classLecturerLock = new Dictionary<string, string>(); // Key: GroupName, Value: LecturerId

            foreach (var req in allRequirements)
            {
                string assignedLecturerId = null;

                // Ưu tiên 1: Lớp này đã có giảng viên được "khóa" chưa?
                if (classLecturerLock.TryGetValue(req.GroupClass.GroupName, out var lockedLecturerId))
                {
                    var capacityKey = (lockedLecturerId, req.Subject.SubjectCode);
                    if (lecturerCapacity.TryGetValue(capacityKey, out int cap) && cap > 0)
                    {
                        assignedLecturerId = lockedLecturerId;
                        lecturerCapacity[capacityKey]--;
                    }
                }
                else // Ưu tiên 2: Tìm giảng viên mới và "khóa" lại
                {
                    var potentialLecturer = lecturerSubjects
                        .FirstOrDefault(ls => ls.SubjectCode == req.Subject.SubjectCode &&
                                             lecturerCapacity.TryGetValue((ls.Lecturer.LecturerId, ls.SubjectCode), out int cap) &&
                                             cap > 0);

                    if (potentialLecturer != null)
                    {
                        assignedLecturerId = potentialLecturer.Lecturer.LecturerId;
                        var capacityKey = (assignedLecturerId, req.Subject.SubjectCode);
                        lecturerCapacity[capacityKey]--;
                        classLecturerLock[req.GroupClass.GroupName] = assignedLecturerId; // Khóa GV cho lớp
                    }
                }

                if (assignedLecturerId != null)
                {
                    successfulAssignments.Add(new Assignment(
                        req.GroupClass,
                        req.Subject,
                        assignedLecturerId,
                        req.GroupClass.GroupName,
                        req.Subject.SubjectCode
                    ));
                }
                else
                {
                    _logger.LogWarning($"Không thể phân công GV cho Lớp: {req.GroupClass.GroupName}, Môn: {req.Subject.SubjectCode}");
                }
            }
            return successfulAssignments;
        }

        #endregion


        #region Giai đoạn 2: Xếp lịch chi tiết

        private List<Schedule> PlaceAssignmentsIntoTimetable(List<Assignment> assignments)
        {
            // 2a. Kiểm tra tải giảng viên (max 5 lớp/buổi/tuần)
            var lecturerLoad = assignments.GroupBy(a => (a.LecturerId, a.GroupClass.PartOfDayInTheFirstTerm))
                                          .ToDictionary(g => g.Key, g => g.Count());
            foreach (var load in lecturerLoad)
            {
                if (load.Value > 5)
                {
                    _logger.LogError($"Xung đột tải: Giảng viên {load.Key.LecturerId} có {load.Value} lớp buổi '{load.Key.PartOfDayInTheFirstTerm}', vượt quá giới hạn 5.");
                    // Có thể dừng hoặc lọc bỏ bớt phân công của giảng viên này
                }
            }

            // 2b. Khởi tạo
            var successfulSchedules = new List<Schedule>();
            var lecturerSchedule = new Dictionary<string, HashSet<int>>();
            var classSchedule = new Dictionary<string, HashSet<int>>();
            var classSubjectLastDay = new Dictionary<(string, string), int>();

            var (morningSlots, afternoonSlots) = DefineTimeSlots();

            // 2c. Vòng lặp xếp lịch
            foreach (var assignment in assignments)
            {
                var availableSlots = assignment.GroupClass.PartOfDayInTheFirstTerm == "A" ? morningSlots : afternoonSlots;
                int determinedSlot = FindBestSlotFor(assignment, availableSlots, lecturerSchedule, classSchedule, classSubjectLastDay);

                if (determinedSlot != 0)
                {
                    successfulSchedules.Add(new Schedule
                    {
                        GroupName = assignment.GroupName,
                        SubjectCode = assignment.SubjectCode,
                        LecturerId = assignment.LecturerId,
                        SlotTime = determinedSlot
                    });
                    UpdateScheduleState(determinedSlot, assignment, lecturerSchedule, classSchedule, classSubjectLastDay);
                }
                else
                {
                    _logger.LogWarning($"Xung đột thời gian: Không tìm được slot cho Lớp {assignment.GroupName}, Môn {assignment.SubjectCode}");
                }
            }
            return successfulSchedules;
        }

        private (List<int> Morning, List<int> Afternoon) DefineTimeSlots()
        {
            var morningSlots = new List<int>();
            var afternoonSlots = new List<int>();

            for (int day = 0; day <= 4; day++)
            {
                for (int slotNum = 1; slotNum <= 2; slotNum++)
                {
                    morningSlots.Add(slotNum);
                    afternoonSlots.Add(slotNum + 2);
                }
            }
            return (morningSlots, afternoonSlots);
        }

        private int FindBestSlotFor(
            Assignment assignment,
            List<int> availableSlots,
            Dictionary<string, HashSet<int>> lecturerSchedule,
            Dictionary<string, HashSet<int>> classSchedule,
            Dictionary<(string, string), int> classSubjectLastDay)
        {
            foreach (var slot in availableSlots)
            {
                // Ràng buộc 1: Giảng viên có bận không?
                if (lecturerSchedule.TryGetValue(assignment.LecturerId, out var lecturerBusySlots) && lecturerBusySlots.Contains(slot))
                    continue;

                // Ràng buộc 2: Lớp học có bận không?
                if (classSchedule.TryGetValue(assignment.GroupName, out var classBusySlots) && classBusySlots.Contains(slot))
                    continue;

                // Ràng buộc 3: Kiểm tra giãn cách môn học
                if (classSubjectLastDay.TryGetValue((assignment.GroupName, assignment.SubjectCode), out int lastDay))
                {
                    int currentDay = slot; // Lấy số từ "T2_..."
                    if (currentDay <= lastDay + 1)
                        continue;
                }

                return slot; // Tìm thấy slot phù hợp
            }
            return 0; // Không tìm thấy
        }

        private void UpdateScheduleState(
            int slot,
            Assignment assignment,
            Dictionary<string, HashSet<int>> lecturerSchedule,
            Dictionary<string, HashSet<int>> classSchedule,
            Dictionary<(string, string), int> classSubjectLastDay)
        {
            if (!lecturerSchedule.ContainsKey(assignment.LecturerId)) lecturerSchedule[assignment.LecturerId] = new HashSet<int>();
            lecturerSchedule[assignment.LecturerId].Add(slot);

            if (!classSchedule.ContainsKey(assignment.GroupName)) classSchedule[assignment.GroupName] = new HashSet<int>();
            classSchedule[assignment.GroupName].Add(slot);

            classSubjectLastDay[(assignment.GroupName, assignment.SubjectCode)] = slot;
        }

        #endregion


    }
}
