//using Microsoft.Extensions.Logging;
//using Microsoft.Windows.Themes;
//using SchedulerWpfApp.Algorithm.DTO;
//using SchedulerWpfApp.Model;
//using Syncfusion.Windows.Controls;
//using System.Collections.Generic;

//namespace SchedulerWpfApp.Algorithm
//{
//    public class ClassSplit
//    {
//        public List<GroupNameStudiedSubject> MorningAllocated { get; set; }
//        public List<GroupNameStudiedSubject> AfternoonAllocated { get; set; }
//    }

//    public class GroupNameStudiedSubject
//    {
//        public GroupClass GroupClass { get; set; }
//        public CurriculumSubject CurriculumSubject { get; set; }
//    }
//    public class CreateScheduleForCommonSubject
//    {
//        private readonly ILogger<CreateScheduleForCommonSubject> _logger;
//        private Dictionary<(string, string), string> _classSubjectLecturerMap;

//        public CreateScheduleForCommonSubject(ILogger<CreateScheduleForCommonSubject> logger)
//        {
//            _logger = logger;
//            _classSubjectLecturerMap = new Dictionary<(string, string), string>();
//        }

//        public List<Schedule> CreateSchedule(List<GroupClass> allGroupClasses,
//            List<LecturerSubject> lecturerSubjects,
//            ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup)
//        {
//            var allSchedules = new List<Schedule>();
//            var lecturerScheduleMap = new Dictionary<string, HashSet<string>>();// Bản đồ theo dõi slot đã gán cho từng giảng viên để tránh conflict

//            // 1. Phân loại lớp theo buổi sáng ("A") và chiều ("P")
//            var morningClasses = allGroupClasses.Where(g => g.PartOfDayInTheFirstTerm == "A").ToList();
//            var afternoonClasses = allGroupClasses.Where(g => g.PartOfDayInTheFirstTerm == "P").ToList();

//            // lấy lớp kỳ 8 kỳ 9 rồi 
//            // có mã môn tìm các lớp học môn đó
//            // có mã môn sẽ tìm được môn đó thuộc khung chương trình nào
//            // có khung chương trình sẽ tìm được các lớp học môn đó

//            // 2. Lấy danh sách tất cả các môn học khác nhau có trong danh sách phân công
//            var subjects = lecturerSubjects.Where(ls => ls.Major == "Common")
//                                           .Select(ls => ls.SubjectCode)
//                                           .Distinct()
//                                           .ToList();
//            var classesAmOfEachLecturer = new Dictionary<Lecturer, List<GroupNameStudiedSubject>>();
//            var classesPmOfEachLecturer = new Dictionary<Lecturer, List<GroupNameStudiedSubject>>();

//            // 3. Duyệt qua từng môn học để phân phối lớp
//            foreach (var subject in subjects)
//            {
//                // Lấy danh sách các giảng viên dạy môn đó
//                var lecturersForSubject = lecturerSubjects.Where(ls => ls.SubjectCode == subject).ToList();

//                // Tổng số lớp cần phân phối cho môn này
//                int totalNeed = lecturersForSubject.Sum(l => l.NumberOfClasses.GetValueOrDefault());
//                int half = totalNeed / 2;

//                // Lọc lớp học của môn này từ danh sách lớp buổi sáng và chiều
//                var morningClassesForSubject = GetClassesForSubject(morningClasses, subject, curriculumLookup);
//                var afternoonClassesForSubject = GetClassesForSubject(afternoonClasses, subject, curriculumLookup);

//                // Chia lớp học sáng/chiều dựa trên tổng số lớp và số lớp thực tế của mỗi buổi
//                var split = AllocateClassesDynamically(morningClassesForSubject, afternoonClassesForSubject, totalNeed, preferMorning: true);
//                var morningAllocated = split.MorningAllocated;
//                var afternoonAllocated = split.AfternoonAllocated;

//                int morningIndex = 0;
//                int afternoonIndex = 0;

//                _logger.LogInformation("Bắt đầu duyệt từng giảng viên dạy môn này để phân lớp ");

//                // Duyệt từng giảng viên dạy môn này để phân lớp
//                foreach (var ls in lecturersForSubject)
//                {
//                    _logger.LogInformation($"Giảng viên: {ls.LecturerName}");

//                    int morningCount = ls.NumberOfClasses.GetValueOrDefault() / 2;
//                    int afternoonCount = ls.NumberOfClasses.GetValueOrDefault() - morningCount;

//                    // Cắt danh sách lớp theo số lượng sáng/chiều đã định
//                    List<GroupNameStudiedSubject> amClassesStudySubject = morningAllocated.Skip(morningIndex).Take(morningCount).ToList();
//                    List<GroupNameStudiedSubject> pmClassesStudySubject = afternoonAllocated.Skip(afternoonIndex).Take(afternoonCount).ToList();

//                    AssignClassesToLecturer(classesAmOfEachLecturer, ls, amClassesStudySubject);
//                    AssignClassesToLecturer(classesPmOfEachLecturer, ls, pmClassesStudySubject);

//                    morningIndex += morningCount;
//                    afternoonIndex += afternoonCount;

//                }
//            }

//            allSchedules.AddRange(GenerateScheduleSafe(classesAmOfEachLecturer, "A"));
//            allSchedules.AddRange(GenerateScheduleSafe(classesPmOfEachLecturer, "P"));

//            return allSchedules;
//        }

//        /// <summary>
//        /// Gán danh sách lớp cho giảng viên và cập nhật map theo dõi.
//        /// </summary>
//        private void AssignClassesToLecturer(Dictionary<Lecturer, List<GroupNameStudiedSubject>> classesOfLecturer, LecturerSubject ls, List<GroupNameStudiedSubject> classesToAssign)
//        {
//            foreach (var classToAssign in classesToAssign)
//            {
//                // KIỂM TRA RÀNG BUỘC 1: Một giảng viên không được dạy 2 môn khác nhau cho 1 lớp
//                // Kiểm tra xem lớp này đã được gán cho GV nào chưa (cho bất kỳ môn nào)
//                var existingAssignment = _classSubjectLecturerMap.Where(kvp => kvp.Key.Item1 == classToAssign.GroupClass.GroupName).ToList();
//                if (existingAssignment.Any() && existingAssignment.First().Value != ls.Lecturer.LecturerId)
//                {
//                    _logger.LogWarning($"Conflict (Requirement 1): Class {classToAssign.GroupClass.GroupName} is already taught by another lecturer. Skipping assignment for {ls.Lecturer.LecturerName} and subject {ls.SubjectCode}.");
//                    continue; // Bỏ qua việc gán lớp này
//                }

//                if (classToAssign.CurriculumSubject.PartOfTerm.Contains("H2")) continue;

//                AddClassToDictionary(classesOfLecturer, ls.Lecturer, classToAssign);
//                // Đánh dấu lớp-môn này đã được gán cho giảng viên
//                _classSubjectLecturerMap[(classToAssign.GroupClass.GroupName, classToAssign.CurriculumSubject.SubjectCode)] = ls.Lecturer.LecturerId;
//            }
//        }

//        // Phương thức dùng để lấy lớp học theo môn học từ các khung chương trình
//        private List<GroupNameStudiedSubject> GetClassesForSubject(List<GroupClass> classes, string subjectCode, ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup)
//        {
//            var result = new List<GroupNameStudiedSubject>();

//            foreach (var groupName in classes)
//            {
//                var curriculumSubjects = curriculumLookup[(groupName.CurriculumCode, groupName.Term.GetValueOrDefault())].ToList();
//                var curriculumSubject = curriculumSubjects.FirstOrDefault(cs => cs.SubjectCode == subjectCode);

//                if (curriculumSubject != null)
//                {
//                    result.Add(new GroupNameStudiedSubject
//                    {
//                        GroupClass = groupName,
//                        CurriculumSubject = curriculumSubject
//                    });
//                }
//            }

//            return result;
//        }

//        // Phương thức thêm lớp vào dictionary cho giảng viên
//        private void AddClassToDictionary(Dictionary<Lecturer, List<GroupNameStudiedSubject>> classesOfLecturer, Lecturer lecturer, GroupNameStudiedSubject classsStudiedSubject)
//        {
//            if (!classesOfLecturer.ContainsKey(lecturer))
//            {
//                classesOfLecturer[lecturer] = new List<GroupNameStudiedSubject>();
//            }

//            classesOfLecturer[lecturer].Add(classsStudiedSubject);
//        }




//        /// <summary>
//        /// Chia lớp theo buổi sáng và chiều dựa trên số lớp có thể xếp ở mỗi buổi.
//        /// Nếu không đủ lớp ở 1 buổi thì phần còn lại sẽ dồn sang buổi kia.
//        /// </summary>
//        private ClassSplit AllocateClassesDynamically(
//            List<GroupNameStudiedSubject> morningPool,
//            List<GroupNameStudiedSubject> afternoonPool,
//            int totalNeed,
//            bool preferMorning = true)
//        {
//            int maxMorning = morningPool.Count;
//            int maxAfternoon = afternoonPool.Count;

//            int idealMorning = totalNeed / 2;
//            int morningCount, afternoonCount;

//            if (preferMorning)
//            {
//                morningCount = Math.Min(idealMorning + totalNeed % 2, maxMorning);
//                afternoonCount = totalNeed - morningCount;
//            }
//            else
//            {
//                afternoonCount = Math.Min(idealMorning + totalNeed % 2, maxAfternoon);
//                morningCount = totalNeed - afternoonCount;
//            }

//            if (morningCount + afternoonCount < totalNeed)
//                throw new InvalidOperationException("Không đủ lớp để phân phối cho môn học này.");

//            return new ClassSplit
//            {
//                MorningAllocated = morningPool.Take(morningCount).ToList(),
//                AfternoonAllocated = afternoonPool.Take(afternoonCount).ToList()
//            };
//        }


//        private List<Schedule> GenerateScheduleSafe(
//            Dictionary<Lecturer, List<GroupNameStudiedSubject>> classesOfEachLecturer,
//            string partOfDay)
//        {
//            var schedules = new List<Schedule>();
//            var tmpSchedules = new List<Schedule>();

//            foreach (Lecturer lecturer in classesOfEachLecturer.Keys)
//            {
//                List<GroupNameStudiedSubject> classes = classesOfEachLecturer[lecturer];
//                int numClasses = classes.Count;

//                if (numClasses < 4)
//                {
//                    continue; // Không có lớp nào cho giảng viên này
//                }
//                else if (numClasses == 4)
//                {
//                    schedules.AddRange(SortLecturerScheduleForFourClasses(lecturer, classes, partOfDay));
//                }
//                else if (numClasses == 5)
//                {
//                    schedules.AddRange(SortLecturerScheduleForFiveClasses(lecturer, classes, partOfDay));
//                }
//            }

//            //foreach (var schedule in tmpSchedules)
//            //{
//            //    if (!lecturerScheduleMap.ContainsKey(lecturer.LecturerId))
//            //        lecturerScheduleMap[lecturer.LecturerId] = new HashSet<string>();

//            //    if (lecturerScheduleMap[lecturer.LecturerId].Contains(schedule.SlotTypeCode))
//            //        throw new Exception($"Conflict at {schedule.SlotTypeCode} for {lecturer.LecturerName}");

//            //    lecturerScheduleMap[lecturer.LecturerId].Add(schedule.SlotTypeCode);
//            //}

//            schedules.AddRange(tmpSchedules);
//            return schedules;
//        }



//        public List<Schedule> SortLecturerScheduleForFiveClasses(
//            Lecturer lecturer,
//            List<GroupNameStudiedSubject> classesStudiedSubjects,
//            string partOfDay) // "A" hoặc "P"
//        {
//            var schedules = new List<Schedule>();

//            int slotStart = partOfDay == ScheduleConstants.PartOfDayIsAM ? ScheduleConstants.NewSlotStartTimeAM : ScheduleConstants.NewSlotStartTimePM; // 1 cho buổi sáng, 3 cho buổi chiều

//            DateTime startDate = new DateTime(2025, 01, 06);

//            // Gọi hàm tái sử dụng để lấy danh sách các ngày trong tuần từ startDate
//            var dayMappings = GetDaysOfWeek(startDate);

//            // Dictionary cho từng ngày, chứa pattern [slot1, slot2] (theo index trong allGroupNames)
//            var dailyPatterns = new Dictionary<int, int[]>
//            {
//                { 0, new int[] { 0, 1 } }, // Class 1 - 5
//                { 1, new int[] { 2, 3 } }, // Class 3 - 4
//                { 2, new int[] { 4, 0 } }, // Class 6 - 1
//                { 3, new int[] { 3, 2 } }, // Class 4 - 3
//                { 4, new int[] { 1, 4 } }, // Class 5 - 6
//            };

//            // Mã hoá slot từng ngày (ví dụ: 2 => thứ 2 slot 1, 4 => thứ 2 slot 2,...)
//            var slotCodeMap = new Dictionary<int, (string S1, string S2)>
//            {
//                { 0, ($"{partOfDay}24", $"{partOfDay}62") },
//                { 1, ($"{partOfDay}35", $"{partOfDay}53") },
//                { 2, ($"{partOfDay}46", $"{partOfDay}24") },
//                { 3, ($"{partOfDay}53", $"{partOfDay}35") },
//                { 4, ($"{partOfDay}62", $"{partOfDay}46") },
//            };



//            foreach (var day in dailyPatterns.Keys)
//            {
//                int[] pattern = dailyPatterns[day];
//                var (slot1Code, slot2Code) = slotCodeMap[day];
//                DateTime currentDate = dayMappings[day]; // Lấy ngày hiện tại từ startDate

//                for (int i = 0; i < 2; i++) // mỗi lượt lặp tương ứng 1 dòng pattern
//                {
//                    var groupName1 = classesStudiedSubjects[pattern[i]].GroupClass.GroupName;
//                    var subject1 = classesStudiedSubjects[pattern[i]].CurriculumSubject.SubjectCode;

//                    try
//                    {
//                        if (i == 0)
//                        {
//                            schedules.Add(CreateScheduleItem(lecturer, subject1, groupName1, currentDate, i + slotStart, slot1Code));
//                        }
//                        else
//                        {
//                            schedules.Add(CreateScheduleItem(lecturer, subject1, groupName1, currentDate, i + slotStart, slot2Code));
//                        }
//                    }
//                    catch (ArgumentException ex)
//                    {
//                        _logger.LogError(ex, "Error creating schedule for {LecturerName} on {Day}", lecturer.LecturerName, day);
//                    }
//                }
//            }

//            return schedules;
//        }

//        public static Dictionary<int, DateTime> GetDaysOfWeek(DateTime startDate)
//        {
//            var daysOfWeek = new Dictionary<int, DateTime>();

//            for (int i = 0; i < 7; i++)
//            {
//                daysOfWeek[i] = startDate.AddDays(i); // Cộng dồn từng ngày
//            }

//            return daysOfWeek;
//        }

//        public List<Schedule> SortLecturerScheduleForFourClasses(Lecturer lecturer, List<GroupNameStudiedSubject> classesStudiedSubjects, string partOfDay) // "A" hoặc "P"
//        {
//            var schedules = new List<Schedule>();

//            DateTime startDate = new DateTime(2025, 01, 06);

//            int slotStart = partOfDay == ScheduleConstants.PartOfDayIsAM ? ScheduleConstants.NewSlotStartTimeAM : ScheduleConstants.NewSlotStartTimePM; // 1 cho buổi sáng, 3 cho buổi chiều

//            // Gọi hàm tái sử dụng để lấy danh sách các ngày trong tuần từ startDate
//            var dayMappings = GetDaysOfWeek(startDate);

//            // Dictionary cho từng ngày, chứa pattern [slot1, slot2] (theo index trong allGroupNames)
//            var dailyPatterns = new Dictionary<int, int[]>
//            {
//                { 0, new int[] { 0, 1 } }, // Class 1 - 2
//                { 1, new int[] { 2, 3 }}, // Class 3 - 4
//                { 2, new int[] { 1, 0  } }, // Class 2 - 1
//                { 3, new int[] { 3, 2 } }, // Class 4 - 3
//            };

//            // Mã hoá slot từng ngày (ví dụ: 2 => thứ 2 slot 1, 4 => thứ 2 slot 2,...)
//            var slotCodeMap = new Dictionary<int, (string S1, string S2)>
//            {
//                { 0, ($"{partOfDay}24", $"{partOfDay}42") },
//                { 1, ($"{partOfDay}35", $"{partOfDay}53") },
//                { 2, ($"{partOfDay}42", $"{partOfDay}24") },
//                { 3, ($"{partOfDay}53", $"{partOfDay}35") },
//            };

//            foreach (var day in dailyPatterns.Keys)
//            {
//                int[] pattern = dailyPatterns[day];
//                var (slot1Code, slot2Code) = slotCodeMap[day];
//                DateTime currentDate = dayMappings[day]; // Lấy ngày hiện tại từ startDate

//                for (int i = 0; i < 2; i++) // mỗi lượt lặp tương ứng 1 dòng pattern
//                {
//                    var groupName1 = classesStudiedSubjects[pattern[i]].GroupClass.GroupName;
//                    var subject1 = classesStudiedSubjects[pattern[i]].CurriculumSubject.SubjectCode;

//                    try
//                    {
//                        if (i == 0)
//                        {
//                            schedules.Add(CreateScheduleItem(lecturer, subject1, groupName1, currentDate, i + slotStart, slot1Code));
//                        }
//                        else
//                        {
//                            schedules.Add(CreateScheduleItem(lecturer, subject1, groupName1, currentDate, i + slotStart, slot2Code));
//                        }
//                    }
//                    catch (ArgumentException ex)
//                    {
//                        _logger.LogError(ex, "Error creating schedule for {LecturerName} on {Day}", lecturer.LecturerName, day);
//                    }

//                }
//            }

//            return schedules;
//        }

//        private Schedule CreateScheduleItem(Lecturer lecturer, string subject, string groupName, DateTime date, int slotTime, string slotTypeCode)
//        {
//            if (lecturer == null || subject == null || string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(slotTypeCode))
//            {
//                throw new ArgumentException("Invalid input parameters for creating schedule.");
//            }
//            return new Schedule
//            {
//                LecturerId = lecturer.LecturerId,
//                LecturerName = lecturer.LecturerName,
//                LecturerAccount = lecturer.LecturerAccount,
//                Date = date,
//                SubjectCode = subject,
//                GroupName = groupName,
//                SlotTime = slotTime,
//                SlotTypeCode = slotTypeCode
//            };
//        }

//    }
//}
