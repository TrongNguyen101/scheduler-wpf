using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using Syncfusion.Data.Extensions;

namespace SchedulerWpfApp.Algorithm.CommonSubject
{
    /// <summary>
    /// Dịch vụ chính để thực hiện việc xếp thời khóa biểu.
    /// </summary>
    public class ScheduleCommonSubjectVersion3
    {
        /// <summary>
        /// Đại diện cho một cặp slot học trong tuần cho một môn.
        /// Dựa trên phân tích từ yêu cầu của bạn:
        /// - A24: Thứ 2 (Day 2) Slot 1 và Thứ 4 (Day 4) Slot 2.
        /// - P35: Thứ 3 (Day 3) Slot 3 và Thứ 5 (Day 5) Slot 4.
        /// </summary>
        private record SlotPair(string Code, int Day1, int Slot1, int Day2, int Slot2, string PartOfDay);

        private readonly List<SlotPair> _availablePairs;

        public ScheduleCommonSubjectVersion3()
        {
            // Khởi tạo các mẫu (pattern) xếp lịch có sẵn dựa trên yêu cầu
            _availablePairs = new List<SlotPair>
            {
                // --- Cặp Slot Buổi Sáng (A) ---
                // Slot 1 & 2
                new SlotPair("A24", 2, 1, 4, 2, "A"), // T2 Slot 1 & T4 Slot 2
                new SlotPair("A42", 4, 1, 2, 2, "A"), // T4 Slot 1 & T2 Slot 2
                new SlotPair("A35", 3, 1, 5, 2, "A"), // T3 Slot 1 & T5 Slot 2
                new SlotPair("A53", 5, 1, 3, 2, "A"), // T5 Slot 1 & T3 Slot 2
                new SlotPair("A26", 2, 1, 6, 2, "A"), // T2 Slot 1 & T6 Slot 2
                new SlotPair("A62", 6, 1, 2, 2, "A"), // T6 Slot 1 & T2 Slot 2
                new SlotPair("A46", 4, 1, 6, 2, "A"), // T4 Slot 1 & T6 Slot 2
                new SlotPair("A64", 6, 1, 4, 2, "A"), // T6 Slot 1 & T4 Slot 2

                // --- Cặp Slot Buổi Chiều (P) ---
                // Slot 3 & 4
                new SlotPair("P24", 2, 3, 4, 4, "P"), // T2 Slot 3 & T4 Slot 4
                new SlotPair("P42", 4, 3, 2, 4, "P"), // T4 Slot 3 & T2 Slot 4
                new SlotPair("P35", 3, 3, 5, 4, "P"), // T3 Slot 3 & T5 Slot 4
                new SlotPair("P53", 5, 3, 3, 4, "P"), // T5 Slot 3 & T3 Slot 4
                new SlotPair("P26", 2, 3, 6, 4, "P"), // T2 Slot 3 & T6 Slot 4
                new SlotPair("P62", 6, 3, 2, 4, "P"), // T6 Slot 3 & T2 Slot 4
                new SlotPair("P46", 4, 3, 6, 4, "P"), // T4 Slot 3 & T6 Slot 4
                new SlotPair("P64", 6, 3, 4, 4, "P"), // T6 Slot 3 & T4 Slot 4
            };
        }

        /// <summary>
        /// Lớp nội bộ để quản lý lịch trình của một tài nguyên (lớp hoặc giảng viên).
        /// </summary>
        private class Timetable
        {
            // Key: (DayOfWeek as int 2-6, Slot as int 1-4)
            private readonly Dictionary<(int, int), Schedule> _scheduledSlots = new Dictionary<(int, int), Schedule>();

            public bool IsSlotFree(int day, int slot) => !_scheduledSlots.ContainsKey((day, slot));

            public void Book(int day, int slot, Schedule schedule) => _scheduledSlots[(day, slot)] = schedule;
        }

        /// <summary>
        /// Hàm chính để tạo ra thời khóa biểu hoàn chỉnh.
        /// </summary>
        /// <param name="groups">Danh sách các lớp học.</param>
        /// <param name="lecturerSubjects">Danh sách phân công giảng viên.</param>
        /// <param name="curriculumSubjects">Danh sách môn học trong chương trình.</param>
        /// <param name="startDate">Ngày bắt đầu của kỳ học (sẽ được dùng để tính ngày cụ thể).</param>
        /// <returns>Một đối tượng chứa danh sách lịch đã xếp và danh sách các xung đột.</returns>
        public (List<Schedule> GeneratedSchedules, List<string> Conflicts) GenerateSchedule(
            List<GroupClass> groups,
            List<LecturerSubject> lecturerSubjects,
            ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup)
        {
            DateTime startDate = new DateTime(2025, 01, 06);
            // --- GIAI ĐOẠN 0: CHUẨN BỊ ---

            // Khởi tạo TKB trống cho mỗi lớp và mỗi giảng viên
            var groupTimetables = groups.ToDictionary(g => g.GroupName, _ => new Timetable());
            var lecturerTimetables = lecturerSubjects.Select(ls => ls.LecturerId).Distinct()
                .ToDictionary(id => id, _ => new Timetable());

            // Tạo danh sách tất cả các đơn vị cần xếp lịch (Unit = Group + Subject)
            var allUnitsToSchedule = new List<(GroupClass Group, CurriculumSubject Subject)>();
            foreach (var groupName in groups)
            {
                var subjectsOfClass = curriculumLookup[(groupName.CurriculumCode, groupName.Term.GetValueOrDefault())].ToList();

                foreach (var subject in subjectsOfClass)
                {
                    if (subject.TeachingMode == ScheduleConstants.TechingModeIsOJT || 
                        subject.TeachingMode == ScheduleConstants.TechingModeIsCoursera || 
                        subject.TeachingMode == ScheduleConstants.TechingModeIsEXE || 
                        subject.TeachingMode == ScheduleConstants.TechingModeIsFullOff ||
                        subject.SubjectCode.Contains("GRA") ||
                        subject.PartOfTerm.Contains("H2"))
                    {
                        continue;
                    }
                    allUnitsToSchedule.Add((groupName, subject));
                }
            }

            // Sắp xếp các unit cần xếp: Ưu tiên "H1" trước, sau đó đến "All", cuối cùng là "H2"
            //var sortedUnits = allUnitsToSchedule
            //    .OrderBy(u => u.Subject.PartOfTerm.StartsWith("H1") ? 0 : (u.Subject.PartOfTerm.StartsWith("All") ? 1 : 2))
            //    .ToList();

            var finalSchedule = new List<Schedule>();
            var conflicts = new List<string>();
            var scheduledUnits = new HashSet<(string, string)>(); // (GroupName, SubjectCode)

            // --- GIAI ĐOẠN 1 & 2: XẾP LỊCH ---

            foreach (var unit in allUnitsToSchedule)
            {
                var (group, subject) = unit;

                // Bỏ qua nếu đã được xếp
                if (scheduledUnits.Contains((group.GroupName, subject.SubjectCode)))
                {
                    continue;
                }

                bool isScheduled = false;

                // Tìm các giảng viên có thể dạy môn này
                var potentialLecturers = lecturerSubjects
                    .Where(ls => ls.SubjectCode == subject.SubjectCode)
                    // Ưu tiên giảng viên dạy nhiều lớp nhất của môn này trước
                    .OrderByDescending(ls => ls.NumberOfClasses)
                    .ToList();

                if (!potentialLecturers.Any())
                {
                    conflicts.Add($"Không tìm thấy giảng viên cho môn {subject.SubjectCode} của lớp {group.GroupName}.");
                    continue;
                }

                // Lấy các cặp slot phù hợp với buổi học yêu cầu của lớp
                var requiredPartOfDay = group.PartOfDayInTheFirstTerm;
                var suitablePairs = _availablePairs.Where(p => p.PartOfDay == requiredPartOfDay);

                // Duyệt qua từng cặp slot và từng giảng viên để tìm chỗ trống
                foreach (var pair in suitablePairs)
                {
                    foreach (var lecturer in potentialLecturers)
                    {
                        var groupTable = groupTimetables[group.GroupName];
                        var lecturerTable = lecturerTimetables[lecturer.LecturerId];

                        // Kiểm tra xem cặp slot này có trống cho cả lớp và giảng viên không
                        if (groupTable.IsSlotFree(pair.Day1, pair.Slot1) &&
                            groupTable.IsSlotFree(pair.Day2, pair.Slot2) &&
                            lecturerTable.IsSlotFree(pair.Day1, pair.Slot1) &&
                            lecturerTable.IsSlotFree(pair.Day2, pair.Slot2))
                        {
                            // --- TÌM THẤY CHỖ TRỐNG ---
                            // Tạo 2 bản ghi Schedule cho cặp slot này
                            var schedule1 = CreateScheduleEntry(group, subject, lecturer, pair, 1, startDate);
                            var schedule2 = CreateScheduleEntry(group, subject, lecturer, pair, 2, startDate);

                            // Thêm vào danh sách kết quả
                            finalSchedule.Add(schedule1);
                            finalSchedule.Add(schedule2);

                            // Đánh dấu vào TKB của lớp và giảng viên
                            groupTable.Book(pair.Day1, pair.Slot1, schedule1);
                            groupTable.Book(pair.Day2, pair.Slot2, schedule2);
                            lecturerTable.Book(pair.Day1, pair.Slot1, schedule1);
                            lecturerTable.Book(pair.Day2, pair.Slot2, schedule2);

                            // Đánh dấu unit này đã được xếp
                            scheduledUnits.Add((group.GroupName, subject.SubjectCode));
                            isScheduled = true;
                            break; // Thoát vòng lặp giảng viên
                        }
                    }
                    if (isScheduled) break; // Thoát vòng lặp cặp slot
                }

                if (!isScheduled)
                {
                    conflicts.Add($"Không thể tìm thấy lịch trống cho môn {subject.SubjectCode} của lớp {group.GroupName}.");
                }
            }

            return (finalSchedule, conflicts);
        }

        /// <summary>
        /// Hàm tiện ích để tạo một đối tượng Schedule.
        /// </summary>
        private Schedule CreateScheduleEntry(GroupClass group, CurriculumSubject subject, LecturerSubject lecturer, SlotPair pair, int part, DateTime startDate)
        {
            int dayOfWeek = (part == 1) ? pair.Day1 : pair.Day2;
            int slotTime = (part == 1) ? pair.Slot1 : pair.Slot2;

            // Tính ngày cụ thể dựa trên startDate và thứ trong tuần
            DateTime scheduleDate = GetDateForDayOfWeek(startDate, (DayOfWeek)(dayOfWeek - 1));

            return new Schedule
            {
                GroupName = group.GroupName,
                SubjectCode = subject.SubjectCode,
                LecturerId = lecturer.LecturerId,
                LecturerName = lecturer.LecturerName,
                LecturerAccount = lecturer.Lecturer.LecturerAccount, // Giả sử LecturerId là tài khoản
                SlotTypeCode = pair.Code,
                PartOfDay = pair.PartOfDay,
                SlotTime = slotTime,
                Date = scheduleDate,
                // Điền các thuộc tính khác nếu cần
                StatusSlot = ScheduleConstants.StatusSlotIsOffline,
                TypeSlot = ScheduleConstants.TypeSlotIsNew,
            };
        }

        /// <summary>
        /// Tính toán ngày cụ thể cho một thứ trong tuần dựa trên ngày bắt đầu.
        /// </summary>
        private DateTime GetDateForDayOfWeek(DateTime startDate, DayOfWeek targetDay)
        {
            int currentDay = (int)startDate.DayOfWeek;
            int targetDayInt = (int)targetDay;
            int daysToAdd = targetDayInt - currentDay;
            if (daysToAdd < 0) daysToAdd += 7; // Nếu ngày mục tiêu đã qua trong tuần này, chuyển sang tuần sau
            return startDate.AddDays(daysToAdd);
        }
    }
}