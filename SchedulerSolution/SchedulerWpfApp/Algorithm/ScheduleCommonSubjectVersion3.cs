using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using Syncfusion.Data.Extensions;

namespace SchedulerWpfApp.Algorithm
{
    public class ScheduleCommonSubjectVersion3
    {
        private record SlotPair(string Code, int Day1, int Slot1, int Day2, int Slot2, string PartOfDay);

        private readonly List<SlotPair> _availablePairs;
        private readonly List<SlotPair> _availablePairsForFiveDays;

        public ScheduleCommonSubjectVersion3()
        {
            _availablePairs = new List<SlotPair>
            {
                // A (morning)
                new SlotPair("A24", 2, 1, 4, 2, "A"),
                new SlotPair("A42", 4, 1, 2, 2, "A"),
                new SlotPair("A35", 3, 1, 5, 2, "A"),
                new SlotPair("A53", 5, 1, 3, 2, "A"),
                new SlotPair("A26", 2, 1, 6, 2, "A"),
                new SlotPair("A62", 6, 1, 2, 2, "A"),
                new SlotPair("A46", 4, 1, 6, 2, "A"),
                new SlotPair("A64", 6, 1, 4, 2, "A"),
                // P (afternoon)
                new SlotPair("P24", 2, 3, 4, 4, "P"),
                new SlotPair("P42", 4, 3, 2, 4, "P"),
                new SlotPair("P35", 3, 3, 5, 4, "P"),
                new SlotPair("P53", 5, 3, 3, 4, "P"),
                new SlotPair("P26", 2, 3, 6, 4, "P"),
                new SlotPair("P62", 6, 3, 2, 4, "P"),
                new SlotPair("P46", 4, 3, 6, 4, "P"),
                new SlotPair("P64", 6, 3, 4, 4, "P"),
            };
            _availablePairsForFiveDays = new List<SlotPair>
            {
                // A (morning)
                new SlotPair("A24", 2, 1, 4, 2, "A"),
                new SlotPair("A62", 6, 1, 2, 2, "A"),
                new SlotPair("A35", 3, 1, 5, 2, "A"),
                new SlotPair("A53", 5, 1, 3, 2, "A"),
                new SlotPair("A46", 4, 1, 6, 2, "A"),

                new SlotPair("A26", 2, 1, 6, 2, "A"),
                new SlotPair("A42", 4, 1, 2, 2, "A"),
                new SlotPair("A53", 5, 1, 3, 2, "A"),
                new SlotPair("A35", 3, 1, 5, 2, "A"),
                new SlotPair("P64", 6, 3, 4, 4, "P"),


                //new SlotPair("A64", 6, 1, 4, 2, "A"),
                // P (afternoon)
                new SlotPair("P24", 2, 3, 4, 4, "P"),
                new SlotPair("P62", 6, 3, 2, 4, "P"),
                new SlotPair("P35", 3, 3, 5, 4, "P"),
                new SlotPair("P53", 5, 3, 3, 4, "P"),
                new SlotPair("P46", 4, 3, 6, 4, "P"),

                new SlotPair("P26", 2, 3, 6, 4, "P"),
                new SlotPair("P42", 4, 3, 2, 4, "P"),
                new SlotPair("A53", 5, 1, 3, 2, "A"),
                new SlotPair("A35", 3, 1, 5, 2, "A"),
                new SlotPair("P64", 6, 3, 4, 4, "P"),
            };
        }

        private class Timetable
        {
            private readonly Dictionary<(int, int), Schedule> _scheduledSlots = new();
            public bool IsSlotFree(int day, int slot) => !_scheduledSlots.ContainsKey((day, slot));
            public void Book(int day, int slot, Schedule schedule) => _scheduledSlots[(day, slot)] = schedule;
        }

        // CHANGED: mở rộng tuple trả về để có thống kê tiến trình
        public (List<Schedule> GeneratedSchedules,
                List<string> Conflicts,
                int TotalPlannedSchedules,
                int TotalUnits,
                int ScheduledUnits,
                int CompletionPercent,
                List<(string GroupName, string SubjectCode)> UnscheduledUnits)
            GenerateSchedule(
                List<GroupClass> groups,
                List<LecturerSubject> lecturerSubjects,
                ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup,
                DateTime startDate,
                IProgress<int> progress)
        {
            // --- PREP ---   

            var groupTimetables = groups.ToDictionary(g => g.GroupName, _ => new Timetable());
            var lecturerTimetables = lecturerSubjects.Select(ls => ls.LecturerId).Distinct()
                .ToDictionary(id => id, _ => new Timetable());

            // capacity per subject           
            var capacityPerSubject = lecturerSubjects
                .GroupBy(ls => (ls.LecturerId, ls.SubjectCode))
                .ToDictionary(g => g.Key, g => g.Max(x => x.NumberOfClasses));

            // overall capacity
            var totalCapacityPerLecturer = lecturerSubjects
                .GroupBy(ls => ls.LecturerId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.NumberOfClasses));

            var assignedPerSubject = new Dictionary<(string LecturerId, string SubjectCode), int>();
            var assignedTotal = new Dictionary<string, int>();

            bool HasCapacity(string lecturerId, string subjectCode)
            {
                capacityPerSubject.TryGetValue((lecturerId, subjectCode), out var subjCap);
                assignedPerSubject.TryGetValue((lecturerId, subjectCode), out var subjUsed);
                if (subjUsed >= subjCap) return false;

                totalCapacityPerLecturer.TryGetValue(lecturerId, out var totalCap);
                assignedTotal.TryGetValue(lecturerId, out var totalUsed);
                if (totalUsed >= totalCap) return false;

                return true;
            }

            void IncreaseLoad(string lecturerId, string subjectCode)
            {
                assignedPerSubject[(lecturerId, subjectCode)] =
                    (assignedPerSubject.TryGetValue((lecturerId, subjectCode), out var v1) ? v1 : 0) + 1;

                assignedTotal[lecturerId] =
                    (assignedTotal.TryGetValue(lecturerId, out var v2) ? v2 : 0) + 1;
            }

            // NEW: xác định danh sách SubjectCode là Common (LecturerSubject.Major == "Common")
            var commonSubjectCodes = lecturerSubjects
                .Where(ls => (ls.Major ?? string.Empty).Trim().Equals("Common", StringComparison.OrdinalIgnoreCase))
                .Select(ls => ls.SubjectCode)
                .ToHashSet();

            // Tạo danh sách Unit = (Group, Subject)
            var totalSubjectsPerGroup = new Dictionary<string, int>();
            var allUnitsToSchedule = new List<(GroupClass Group, CurriculumSubject Subject)>();
            foreach (var g in groups)
            {
                var subjectsOfClass = curriculumLookup[(g.CurriculumCode, g.Term.GetValueOrDefault())].ToList();
                // Lọc môn không hợp lệ
                var validSubjects = subjectsOfClass
                    .Where(subject =>
                        subject.TeachingMode != ScheduleConstants.TechingModeIsOJT &&
                        subject.TeachingMode != ScheduleConstants.TechingModeIsCoursera &&
                        subject.TeachingMode != ScheduleConstants.TechingModeIsEXE &&
                        subject.TeachingMode != ScheduleConstants.TechingModeIsFullOff &&
                        !subject.SubjectCode.Contains("GRA") &&
                        !subject.PartOfTerm.Contains("H2"))
                    .ToList();

                // Thêm vào danh sách unit
                foreach (var subject in validSubjects)
                {
                    allUnitsToSchedule.Add((g, subject));
                }

                // Lưu tổng số môn hợp lệ của group
                totalSubjectsPerGroup[g.GroupName] = validSubjects.Count;
            }

            // NEW: ưu tiên Common ở kỳ 8–9 trước, sau đó giữ nguyên ưu tiên term giảm dần
            var sortedUnits = allUnitsToSchedule
                .OrderByDescending(u =>
                    (u.Group.Term == 8 || u.Group.Term == 9) &&
                    commonSubjectCodes.Contains(u.Subject.SubjectCode)) // true -> 1, false -> 0
                .ThenByDescending(u => u.Group.Term)
                .ToList();

            var finalSchedule = new List<Schedule>();
            var conflicts = new List<string>();
            var unscheduledUnits = new List<(string GroupName, string SubjectCode)>();
            var scheduledUnitsSet = new HashSet<(string, string)>(); // (GroupName, SubjectCode)

            // NEW: tổng số đối tượng lịch dự kiến tạo (mỗi unit = 2 bản ghi Schedule)
            int totalUnits = sortedUnits.Count;
            int totalPlannedSchedules = totalUnits * 2;

            foreach (var unit in sortedUnits)
            {
                var (group, subject) = unit;

                if (scheduledUnitsSet.Contains((group.GroupName, subject.SubjectCode))) continue;

                bool isScheduled = false;

                // Tìm GV có thể dạy môn + còn capacity
                var potentialLecturers = lecturerSubjects
                    .Where(ls => ls.SubjectCode == subject.SubjectCode)
                    .ToList();

                var lecturersWithCapacity = potentialLecturers
                    .Where(ls => HasCapacity(ls.LecturerId, subject.SubjectCode))
                    // NEW: ưu tiên GV có tổng tải = 2 lớp, sau đó NumberOfClasses giảm dần
                    .OrderByDescending(ls =>
                        (totalCapacityPerLecturer.TryGetValue(ls.LecturerId, out var cap) ? cap : 0) == 2)
                    .ThenByDescending(ls => ls.NumberOfClasses)
                    .ToList();

                if (!lecturersWithCapacity.Any())
                {
                    conflicts.Add($"Môn {subject.SubjectCode} - lớp {group.GroupName}: tất cả giảng viên đã đầy tải (theo môn hoặc tổng).");
                    unscheduledUnits.Add((group.GroupName, subject.SubjectCode));
                    continue;
                }

                var majorSubject = lecturerSubjects.FirstOrDefault(l => l.SubjectCode == subject.SubjectCode);

                var requiredPartOfDay = group.PartOfDayInTheFirstTerm;
                List<SlotPair> suitablePairs;

                if (totalSubjectsPerGroup[group.GroupName] == 5)
                {
                    suitablePairs = _availablePairsForFiveDays.Where(p => p.PartOfDay == requiredPartOfDay).ToList();
                }
                else
                {
                    suitablePairs = _availablePairs.Where(p => p.PartOfDay == requiredPartOfDay).ToList();
                }


                // === Nâng cấp phần xếp lịch ===
                foreach (var lecturer in lecturersWithCapacity)
                {

                    foreach (var pair in suitablePairs)
                    {
                        if ((lecturer.Lecturer.Role ?? string.Empty).Trim() == "TBM" && pair.Code == "A53")
                            continue;

                        var groupTable = groupTimetables[group.GroupName];
                        var lecturerTable = lecturerTimetables[lecturer.LecturerId];

                        // Case 1: cả lớp và GV đều trống ở cả 2 ca -> xếp hoàn chỉnh
                        if (groupTable.IsSlotFree(pair.Day1, pair.Slot1) &&
                            groupTable.IsSlotFree(pair.Day2, pair.Slot2) &&
                            lecturerTable.IsSlotFree(pair.Day1, pair.Slot1) &&
                            lecturerTable.IsSlotFree(pair.Day2, pair.Slot2))
                        {
                            var schedule1 = CreateScheduleEntry(group, subject, lecturer, pair, 1, startDate);
                            var schedule2 = CreateScheduleEntry(group, subject, lecturer, pair, 2, startDate);

                            finalSchedule.Add(schedule1);
                            finalSchedule.Add(schedule2);

                            groupTable.Book(pair.Day1, pair.Slot1, schedule1);
                            groupTable.Book(pair.Day2, pair.Slot2, schedule2);
                            lecturerTable.Book(pair.Day1, pair.Slot1, schedule1);
                            lecturerTable.Book(pair.Day2, pair.Slot2, schedule2);

                            scheduledUnitsSet.Add((group.GroupName, subject.SubjectCode));
                            IncreaseLoad(lecturer.LecturerId, subject.SubjectCode);
                            isScheduled = true;
                            break;
                        }

                        // KHÔNG còn nhánh else-if ở đây nữa
                    }

                    if (isScheduled) break;
                }

                //// Fallback: sau khi thử hết giảng viên mà vẫn chưa xếp được
                if (!isScheduled)
                {
                    var groupTable = groupTimetables[group.GroupName];

                    // Chọn cặp slot phù hợp buổi mà GROUP trống cả 2 ca
                    var fallbackPair = suitablePairs.FirstOrDefault(p =>
                        groupTable.IsSlotFree(p.Day1, p.Slot1) &&
                        groupTable.IsSlotFree(p.Day2, p.Slot2));

                    if (fallbackPair != null)
                    {
                        // Tạo lịch với giảng viên rỗng ("")
                        var schedule1 = CreateScheduleEntry(group, subject, null, fallbackPair, 1, startDate);
                        var schedule2 = CreateScheduleEntry(group, subject, null, fallbackPair, 2, startDate);

                        finalSchedule.Add(schedule1);
                        finalSchedule.Add(schedule2);

                        // Chỉ book vào GROUP, không book vào giảng viên
                        groupTable.Book(fallbackPair.Day1, fallbackPair.Slot1, schedule1);
                        groupTable.Book(fallbackPair.Day2, fallbackPair.Slot2, schedule2);

                        //scheduledUnitsSet.Add((group.GroupName, subject.SubjectCode));

                        // Ghi conflict “slot chưa có giảng viên”
                        conflicts.Add(
                            $"Lớp {group.GroupName} - môn {subject.SubjectCode}: chưa gán giảng viên, đã giữ chỗ cặp {fallbackPair.Code} " +
                            $"(D{fallbackPair.Day1}-S{fallbackPair.Slot1} & D{fallbackPair.Day2}-S{fallbackPair.Slot2}).");

                        isScheduled = true;
                    }
                    else
                    {
                        // Không còn slot trống cho group
                        conflicts.Add($"Không thể tìm thấy lịch trống cho môn {subject.SubjectCode} của lớp {group.GroupName}.");
                        unscheduledUnits.Add((group.GroupName, subject.SubjectCode));
                    }
                }


                if (!isScheduled)
                {
                    conflicts.Add($"Không thể tìm thấy lịch trống cho môn {subject.SubjectCode} của lớp {group.GroupName}.");
                    unscheduledUnits.Add((group.GroupName, subject.SubjectCode));
                }
            }

            // NEW: % hoàn tất theo Unit (phù hợp thanh tiến trình)
            int scheduledUnits = scheduledUnitsSet.Count;
            int completionPercent = totalUnits == 0 ? 100 : (int)((double)scheduledUnits / totalUnits * 100);

            return (finalSchedule,
                    conflicts,
                    totalPlannedSchedules,
                    totalUnits,
                    scheduledUnits,
                    completionPercent,
                    unscheduledUnits);
        }

        private Schedule CreateScheduleEntry(GroupClass groupName, CurriculumSubject curriculumSubject, LecturerSubject lecturer, SlotPair pair, int part, DateTime startDate)
        {
            int dayOfWeek = part == 1 ? pair.Day1 : pair.Day2;
            int slotTime = part == 1 ? pair.Slot1 : pair.Slot2;

            DateTime scheduleDate = GetDateForDayOfWeek(startDate, (DayOfWeek)(dayOfWeek - 1));

            return new Schedule
            {
                GroupName = groupName.GroupName,
                SubjectCode = curriculumSubject.SubjectCode,
                LecturerId = lecturer?.LecturerId ?? "",
                LecturerName = lecturer?.LecturerName ?? "",
                LecturerAccount = lecturer?.Lecturer?.LecturerAccount ?? "",
                SlotTypeCode = pair.Code,
                PartOfDay = pair.PartOfDay,
                SlotTime = slotTime,
                SessionNo = part,
                Date = scheduleDate,
                Major = curriculumSubject.CurriculumCode,
                StatusSlot = ScheduleConstants.StatusSlotIsOffline,
                TypeSlot = ScheduleConstants.TypeSlotIsNew,
            };
        }

        private DateTime GetDateForDayOfWeek(DateTime startDate, DayOfWeek targetDay)
        {
            int currentDay = (int)startDate.DayOfWeek;
            int targetDayInt = (int)targetDay;
            int daysToAdd = targetDayInt - currentDay;
            if (daysToAdd < 0) daysToAdd += 7;
            return startDate.AddDays(daysToAdd);
        }
    }
}
