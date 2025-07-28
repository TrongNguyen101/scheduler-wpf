using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    /// <summary>
    /// Dịch vụ chịu trách nhiệm tạo thời khóa biểu dựa trên các quy tắc và dữ liệu đầu vào.
    /// </summary>
    public class SchedulingService
    {
        // Danh sách dữ liệu đầu vào
        private List<GroupClass> _groupClasses;
        private List<CurriculumSubject> _curriculumSubjects;
        private List<LecturerSubject> _lecturerSubjects;
        private List<Room> _rooms;

        // Các cấu trúc dữ liệu để theo dõi trạng thái của lịch trình trong một tuần
        // Key: GroupName, Value: Tập hợp các "khóa thời gian" đã bị chiếm (ví dụ: "A-Monday-1")
        private Dictionary<string, HashSet<string>> _classScheduleTracker;

        // Key: LecturerId, Value: Tập hợp các "khóa thời gian" đã bị chiếm
        private Dictionary<string, HashSet<string>> _lecturerScheduleTracker;

        // Key: RoomId, Value: Tập hợp các "khóa thời gian" đã bị chiếm
        private Dictionary<int, HashSet<string>> _roomScheduleTracker;

        // Key: GroupName, Value: (Key: SubjectCode, Value: LecturerId)
        // Để đảm bảo một giảng viên chỉ dạy 1 môn cho 1 lớp
        private Dictionary<string, Dictionary<string, string>> _lecturerClassAssignment;

        public List<Schedule> InitalData(
            List<GroupClass> groupClasses,
            List<CurriculumSubject> curriculumSubjects,
            List<LecturerSubject> lecturerSubjects,
            List<Room> rooms)
        {
            _groupClasses = groupClasses;
            _curriculumSubjects = curriculumSubjects;
            _lecturerSubjects = lecturerSubjects;
            _rooms = rooms;
            DateTime startDate = new DateTime(2025, 01, 06);

            var fullSchedule = GenerateFullSchedule(startDate, 10); // Giả sử xếp lịch cho 16 tuần
            return fullSchedule;
        }

        /// <summary>
        /// Hàm chính để tạo ra một thời khóa biểu hoàn chỉnh cho nhiều tuần.
        /// </summary>
        /// <param name="termStartDate">Ngày bắt đầu của kỳ học. Nên là một ngày Thứ Hai.</param>
        /// <param name="numberOfWeeks">Tổng số tuần cần xếp lịch.</param>
        /// <returns>Danh sách các buổi học đã được xếp.</returns>
        public List<Schedule> GenerateFullSchedule(DateTime termStartDate, int numberOfWeeks)
        {
            // 1. Tạo ra một lịch mẫu cho 1 tuần duy nhất.
            List<Schedule> weeklyScheduleTemplate = GenerateWeeklyScheduleTemplate();

            // 2. Từ lịch mẫu, nhân bản ra cho N tuần với ngày tháng cụ thể.
            List<Schedule> fullSchedule = new List<Schedule>();
            for (int week = 0; week < numberOfWeeks; week++)
            {
                foreach (var templateSlot in weeklyScheduleTemplate)
                {
                    // Tạo một bản sao của slot mẫu
                    var concreteSchedule = CreateScheduleTemplate(templateSlot);

                    // Tính toán ngày thực tế cho buổi học
                    int dayOffset = (int)templateSlot.Date.Value.DayOfWeek - 1; // Monday=0, Tuesday=1,...
                    concreteSchedule.Date = termStartDate.AddDays(week * 7 + dayOffset);

                    // Cập nhật các thông tin liên quan đến tuần (nếu cần)
                    // Ví dụ: concreteSchedule.WeekNumber = week + 1;

                    fullSchedule.Add(concreteSchedule);
                }
            }

            return weeklyScheduleTemplate;
        }

        private Schedule CreateScheduleTemplate (Schedule schedule)
        {
            var scheduleTemplate = new Schedule
            {
                ScheduleId = schedule.ScheduleId,
                RoomId = schedule.RoomId,
                RoomName = schedule.RoomName,
                PartOfDay = schedule.PartOfDay,
                SlotTime = schedule.SlotTime,
                StatusSlot = schedule.StatusSlot,
                Date = schedule.Date,
                Major = schedule.Major,
                SubjectCode = schedule.SubjectCode,
                GroupName = schedule.GroupName,
                LecturerId = schedule.LecturerId,
                LecturerName = schedule.LecturerName,
                LecturerAccount = schedule.LecturerAccount,
                TypeSlot = schedule.TypeSlot,
                SessionNo = schedule.SessionNo,
                SlotTypeCode = schedule.SlotTypeCode,
                TermInYear = schedule.TermInYear,
            };
            return scheduleTemplate;
        }

        /// <summary>
        /// Tạo ra một lịch mẫu cho một tuần. Đây là logic cốt lõi.
        /// </summary>
        private List<Schedule> GenerateWeeklyScheduleTemplate()
        {
            // Khởi tạo các tracker
            InitializeTrackers();

            var finalWeeklySchedule = new List<Schedule>();

            // Lấy danh sách các cặp slot hợp lệ (cách nhau ít nhất 1 ngày)
            var validSlotPairs = GetValidSlotPairs();

            // Ưu tiên xếp các môn nửa kỳ đầu (H1), sau đó đến các môn cả kỳ (All)
            var subjectPriority = new[] { "H1", "All", "H2" };

            // **Bắt đầu thuật toán Greedy: Duyệt qua từng lớp để đảm bảo không bỏ sót**
            foreach (var groupClass in _groupClasses)
            {
                // Lấy danh sách các môn học của lớp này từ khung chương trình
                var subjectsForClass = _curriculumSubjects
                    .Where(cs => cs.CurriculumCode == groupClass.CurriculumCode && cs.TermNo == groupClass.Term)
                    .OrderBy(cs => Array.IndexOf(subjectPriority, cs.PartOfTerm)) // Sắp xếp theo ưu tiên kỳ học
                    .ToList();

                foreach (var subject in subjectsForClass)
                {
                    bool scheduled = false;

                    // **Tìm kiếm giảng viên phù hợp cho môn học**
                    var potentialLecturers = _lecturerSubjects
                        .Where(ls => ls.SubjectCode == subject.SubjectCode)
                        .ToList();

                    foreach (var lecturer in potentialLecturers)
                    {
                        // RÀNG BUỘC: Giảng viên này đã dạy môn khác cho lớp này chưa?
                        if (_lecturerClassAssignment[groupClass.GroupName].ContainsKey(lecturer.LecturerId) &&
                            _lecturerClassAssignment[groupClass.GroupName][lecturer.LecturerId] != subject.SubjectCode)
                        {
                            continue; // Bỏ qua, giảng viên này đã dạy môn khác cho lớp này rồi.
                        }

                        // **Tìm kiếm một cặp slot trống cho cả lớp và giảng viên**
                        foreach (var pair in validSlotPairs)
                        {
                            // Tạo "khóa thời gian" cho 2 slot trong cặp
                            string partOfDay = groupClass.PartOfDayInTheFirstTerm; // "A" hoặc "P"
                            string timeKey1 = $"{partOfDay}-{pair.Slot1.Day}-{pair.Slot1.SlotInDay}";
                            string timeKey2 = $"{partOfDay}-{pair.Slot2.Day}-{pair.Slot2.SlotInDay}";

                            // KIỂM TRA TÍNH SẴN SÀNG: Cả lớp và giảng viên đều phải rảnh vào 2 slot này
                            if (IsAvailable(_classScheduleTracker, groupClass.GroupName, timeKey1, timeKey2) &&
                                IsAvailable(_lecturerScheduleTracker, lecturer.LecturerId, timeKey1, timeKey2))
                            {
                                // **Khi đã tìm được giờ, tiến hành tìm phòng**
                                var scheduleTemplate = new Schedule
                                {
                                    GroupName = groupClass.GroupName,
                                    SubjectCode = subject.SubjectCode,
                                    Major = groupClass.Major,
                                    PartOfDay = partOfDay,
                                    LecturerId = lecturer.LecturerId,
                                    LecturerName = lecturer.LecturerName,
                                    StatusSlot = "OFF" // Giả sử mặc định là offline
                                };

                                Room availableRoom = FindAvailableRoom(scheduleTemplate, timeKey1, timeKey2);

                                if (availableRoom != null)
                                {
                                    // **CHỐT LỊCH: Đã tìm thấy GV, Giờ, và Phòng**
                                    // Tạo 2 đối tượng Schedule cho 2 buổi học trong tuần
                                    var schedule1 = CreateScheduleEntry(scheduleTemplate, pair.Slot1, availableRoom, 1);
                                    var schedule2 = CreateScheduleEntry(scheduleTemplate, pair.Slot2, availableRoom, 2);

                                    finalWeeklySchedule.Add(schedule1);
                                    finalWeeklySchedule.Add(schedule2);

                                    // Cập nhật trạng thái đã bị chiếm
                                    BookSlot(_classScheduleTracker, groupClass.GroupName, timeKey1, timeKey2);
                                    BookSlot(_lecturerScheduleTracker, lecturer.LecturerId, timeKey1, timeKey2);
                                    BookSlot(_roomScheduleTracker, availableRoom.RoomId, timeKey1, timeKey2);

                                    // Ghi nhận giảng viên dạy môn này cho lớp này
                                    if (!_lecturerClassAssignment[groupClass.GroupName].ContainsKey(lecturer.LecturerId))
                                    {
                                        _lecturerClassAssignment[groupClass.GroupName].Add(lecturer.LecturerId, subject.SubjectCode);
                                    }


                                    scheduled = true;
                                    break; // Thoát khỏi vòng lặp tìm slot
                                }
                            }
                        } // Kết thúc vòng lặp tìm slot

                        if (scheduled)
                        {
                            break; // Thoát khỏi vòng lặp tìm giảng viên
                        }
                    } // Kết thúc vòng lặp tìm giảng viên

                    if (!scheduled)
                    {
                        // Xử lý trường hợp không thể xếp lịch cho môn học này (ghi log, báo lỗi,...)
                        Console.WriteLine($"WARNING: Could not schedule Subject '{subject.SubjectCode}' for Class '{groupClass.GroupName}'.");
                    }
                } // Kết thúc vòng lặp các môn học
            } // Kết thúc vòng lặp các lớp

            return finalWeeklySchedule;
        }

        #region Helper Methods

        /// <summary>
        /// Khởi tạo các dictionary theo dõi trạng thái.
        /// </summary>
        private void InitializeTrackers()
        {
            _classScheduleTracker = _groupClasses.ToDictionary(g => g.GroupName, g => new HashSet<string>());
            _lecturerScheduleTracker = _lecturerSubjects.Select(l => l.LecturerId).Distinct()
                                         .ToDictionary(id => id, id => new HashSet<string>());
            _roomScheduleTracker = _rooms.ToDictionary(r => r.RoomId, r => new HashSet<string>());
            _lecturerClassAssignment = _groupClasses.ToDictionary(g => g.GroupName, g => new Dictionary<string, string>());
        }

        /// <summary>
        /// Tìm phòng trống phù hợp với các yêu cầu.
        /// </summary>
        private Room FindAvailableRoom(Schedule schedule, string timeKey1, string timeKey2)
        {
            // Danh sách các chuyên ngành yêu cầu học ở tòa nhà Gamma
            var gammaMajors = new HashSet<string> { "FN", "HM", "MC", "BA", "TM", "IB", "EC", "AI", "SE" };

            List<Room> potentialRooms;
            if (gammaMajors.Contains(schedule.Major))
            {
                // Lọc các phòng thuộc tòa nhà 'G' (Gamma)
                potentialRooms = _rooms.Where(r => r.Building.Contains("G")).ToList();
            }
            else
            {
                // Các chuyên ngành khác có thể học ở bất kỳ phòng nào
                potentialRooms = _rooms;
            }

            // Tìm phòng đầu tiên rảnh cả 2 slot
            foreach (var room in potentialRooms)
            {
                if (IsAvailable(_roomScheduleTracker, room.RoomId, timeKey1, timeKey2))
                {
                    return room;
                }
            }

            return null; // Không tìm thấy phòng phù hợp
        }

        /// <summary>
        /// Tạo một đối tượng Schedule hoàn chỉnh cho một buổi học.
        /// </summary>
        private Schedule CreateScheduleEntry(Schedule template, (DayOfWeek Day, int SlotInDay) slotInfo, Room room, int sessionNo)
        {
            // Giả lập ngày trong tuần để dùng cho việc nhân bản sau này. Monday = Day 1
            var fakeDate = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek + (int)slotInfo.Day);

            return new Schedule
            {
                GroupName = template.GroupName,
                SubjectCode = template.SubjectCode,
                Major = template.Major,
                LecturerId = template.LecturerId,
                LecturerName = template.LecturerName,
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                PartOfDay = template.PartOfDay, // 'A' hoặc 'P'
                SlotTime = (template.PartOfDay == "A") ? slotInfo.SlotInDay : slotInfo.SlotInDay + 2, // Sáng là 1,2; Chiều là 3,4
                Date = fakeDate,
                SessionNo = sessionNo, // Đánh dấu buổi 1 hay 2 trong tuần
                StatusSlot = template.StatusSlot
            };
        }

        /// <summary>
        /// Kiểm tra xem một thực thể (lớp, GV, phòng) có rảnh vào cả 2 slot không.
        /// </summary>
        private bool IsAvailable<T>(Dictionary<T, HashSet<string>> tracker, T key, string timeKey1, string timeKey2)
        {
            return !tracker[key].Contains(timeKey1) && !tracker[key].Contains(timeKey2);
        }

        /// <summary>
        /// Đánh dấu 2 slot là đã bị chiếm.
        /// </summary>
        private void BookSlot<T>(Dictionary<T, HashSet<string>> tracker, T key, string timeKey1, string timeKey2)
        {
            tracker[key].Add(timeKey1);
            tracker[key].Add(timeKey2);
        }

        /// <summary>
        /// Trả về danh sách các cặp slot hợp lệ trong tuần (cách nhau ít nhất 1 ngày).
        /// </summary>
        private List<((DayOfWeek Day, int SlotInDay) Slot1, (DayOfWeek Day, int SlotInDay) Slot2)> GetValidSlotPairs()
        {
            var pairs = new List<((DayOfWeek, int), (DayOfWeek, int))>();
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

            // Mỗi buổi có 2 slot (slot 1 và 2 trong buổi)
            for (int i = 1; i <= 2; i++)
            {
                // Thứ 2 đi với Thứ 4, 5
                pairs.Add(((days[0], i), (days[2], i)));
                pairs.Add(((days[0], i), (days[3], i)));
                pairs.Add(((days[0], i), (days[4], i)));

                // Thứ 3 đi với Thứ 5
                pairs.Add(((days[1], i), (days[3], i)));
                pairs.Add(((days[1], i), (days[4], i)));

                // Thứ 4 đi với Thứ 6 (nếu có học T6)
                pairs.Add(((days[2], i), (days[4], i)));
            }

            // Đây là ví dụ về các cặp slot chéo (slot 1 của ngày này với slot 2 của ngày kia)
            // Dựa theo pattern bạn cung cấp (A24, A35,...)
            // A24 -> Sáng, Thứ 2-Slot 1 & Thứ 4-Slot 2
            pairs.Add(((DayOfWeek.Monday, 1), (DayOfWeek.Wednesday, 2)));
            // A42 -> Sáng, Thứ 4-Slot 1 & Thứ 2-Slot 2
            pairs.Add(((DayOfWeek.Wednesday, 1), (DayOfWeek.Monday, 2)));

            // A35 -> Sáng, Thứ 3-Slot 1 & Thứ 5-Slot 2
            pairs.Add(((DayOfWeek.Tuesday, 1), (DayOfWeek.Thursday, 2)));
            // A53 -> Sáng, Thứ 5-Slot 1 & Thứ 3-Slot 2
            pairs.Add(((DayOfWeek.Thursday, 1), (DayOfWeek.Tuesday, 2)));

            return pairs;
        }

        #endregion
    }
}