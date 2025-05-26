using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class GenerateScheduleForAllDate
    {
        private readonly TreeForSchedule _treeNode;
        private readonly SortSubjectsOneSession _sortSubjectsOneSession;
        private readonly GetLecturerForSubject _getLecturerForSubject;

        private static readonly Dictionary<int, string> Slot1Map = new()
        {
            { 2, "24" },
            { 3, "35" },
            { 4, "42" },
            { 5, "53" },
            { 6, "C" }
        };

        private static readonly Dictionary<int, string> Slot2Map = new()
        {
            { 2, "42" },
            { 3, "53" },
            { 4, "24" },
            { 5, "35" },
            { 6, "C" }
        };

        public GenerateScheduleForAllDate(TreeForSchedule treeNode, GetLecturerForSubject getLecturerForSubject, SortSubjectsOneSession sortSubjectsOneSession)
        {
            _treeNode = treeNode;
            _sortSubjectsOneSession = sortSubjectsOneSession;
            _getLecturerForSubject = getLecturerForSubject;
        }
        public void CreateSchedules(List<Schedule> allSchedules, List<Subject> subjects, int numberOfClass, List<LecturerSubject> lecturerSubject, DateTime startDate, List<LecturerRequest> lecturerRequests)
        {
            try
            {
                // Calculate the number of rooms needed based on the number of classes
                int numberOfRoom = CalculateNumberOfRooms(numberOfClass);

                // Cache lecturer lookup
                // có bao nhiêu ông thầy thì có bấy nhiêu lớp học cùng lúc
                var lecturersTeachSubject = lecturerSubject.GroupBy(l => l.SubjectCode).ToDictionary(g => g.Key, g => g.ToList());
                var lecturersAM = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "AM");
                var lecturersPM = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "PM");

                CreateScheduleForSession(allSchedules, subjects, lecturersAM, numberOfRoom, startDate, lecturerRequests, "A", "G", 1);
                CreateScheduleForSession(allSchedules, subjects, lecturersPM, numberOfRoom, startDate, lecturerRequests, "P", "G", numberOfRoom + 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating schedules: {ex}");
            }
        }

        /// <summary>
        /// Tạo lịch cho một session (AM hoặc PM) cho tất cả các phòng, lớp, tuần và ngày.
        /// - Duyệt qua từng phòng (room), mỗi phòng tương ứng với một lớp học (class).
        /// - Với mỗi phòng, xây dựng cây lịch cho phòng đó.
        /// - Duyệt qua 10 tuần, mỗi tuần xác định kiểu slot (online/offline).
        /// - Duyệt qua 7 ngày trong tuần, xác định ngày hiện tại.
        /// - Lọc danh sách giảng viên phù hợp cho ngày và session hiện tại.
        /// - Duyệt qua 2 slot của mỗi buổi (AM/PM), lấy môn học đã được sắp xếp cho slot đó.
        /// - Sinh mã loại slot (slotTypeCode) dựa trên ngày, slot và session.
        /// - Tính số thứ tự buổi học (sessionNo) của môn học trong kỳ.
        /// - Tìm tên giảng viên phù hợp cho môn học, lớp và chu kỳ hiện tại.
        /// - Thu thập và thêm lịch trình vào danh sách allSchedules.
        /// </summary>
        /// <param name="allSchedules">Danh sách lưu trữ tất cả các lịch trình được tạo.</param>
        /// <param name="subjects">Danh sách các môn học.</param>
        /// <param name="lecturersTeachSubjectSession">Dictionary chứa danh sách giảng viên theo môn học cho session hiện tại.</param>
        /// <param name="numberOfRoom">Số lượng phòng cần tạo lịch.</param>
        /// <param name="startDate">Ngày bắt đầu của kỳ học.</param>
        /// <param name="lecturerRequests">Danh sách yêu cầu của giảng viên.</param>
        /// <param name="sessionFilter">Session hiện tại ("A" cho AM, "P" cho PM).</param>
        /// <param name="roomCodePrefix">Tiền tố mã phòng (ví dụ: "G").</param>
        /// <param name="classIdStartIndex">Chỉ số bắt đầu để sinh mã lớp.</param>
        private void CreateScheduleForSession(List<Schedule> allSchedules,
                                      List<Subject> subjects,
                                      Dictionary<string, List<LecturerSubject>> lecturersTeachSubjectSession,
                                      int numberOfRoom,
                                      DateTime startDate,
                                      List<LecturerRequest> lecturerRequests,
                                      string sessionFilter,
                                      string roomCodePrefix,
                                      int classIdStartIndex)
        {
            var subjectSchedule = _sortSubjectsOneSession.SortSubjectFourClass(subjects);

            // Dictionary dùng để tạo thứ tự từng slot học trong kỳ
            var subjectAppearanceOrder = new Dictionary<string, int>();

            // số thứ tự slot dựa vào buổi trong ngày
            int slotStart = sessionFilter == "A" ? 1 : 3;

            // số lượng slot trong 1 buổi
            int slotsPerSession = 2;

            for (int roomNo = 1; roomNo <= numberOfRoom; roomNo++)
            {
                /* Cần hàm tạo room Id ở đây*/
                var roomId = $"{roomCodePrefix}{roomNo}";
                TreeForSchedule roomNode = _treeNode.BuildTreeForRoom(roomId);

                /* Cần hàm tạo group name (mã lơp) ở đây*/
                string classId = $"SE160{classIdStartIndex + roomNo - 1}";

                // tìm thầy cho mỗi 4 lớp
                var (classIndex, cycleLevel) = MapToCycle(roomNo);

                // duyệt qua 10 tuần
                for (int week = 1; week <= 10; week++)
                {
                    // Tạo kiểu onl hay off cho tuần đó
                    string slotType = GetSlotTypeForWeek(week);

                    //Duyệt qua 7 ngày trong tuần
                    for (int dayOfWeek = 1; dayOfWeek <= 7; dayOfWeek++)
                    {
                        //Lấy ngày tháng hiện tại của ngày
                        DateTime currentDate = startDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));

                        // duyệt qua 2 slot của 1 buổi
                        for (int slotIndex = 0; slotIndex < slotsPerSession; slotIndex++)
                        {
                            // Lấy môn học đã được xếp vào ngày slot hiện tại
                            var subject = subjectSchedule[dayOfWeek, classIndex, slotIndex];
                            if (subject == null) continue;

                            // Lấy mã loại slot dựa trên ngày, slot và buổi
                            string slotTypeCode = GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, sessionFilter);

                            // Lấy thứ tự buổi học trong kỳ
                            int sessionNo = GetSessionNo(subjectAppearanceOrder, subject);

                            // lấy tên giảng viên để thêm vào lịch
                            var lecturerName = _getLecturerForSubject.FindLecturerForSubject(
                                subject?.SubjectCode, lecturersTeachSubjectSession, cycleLevel);

                            string slotLabel = $"slot {slotIndex + slotStart}";

                            _treeNode.CollectSchedules(
                                roomNode,
                                allSchedules,
                                subject.SubjectCode,
                                currentDate,
                                classId,
                                slotLabel,
                                lecturerName,
                                slotTypeCode,
                                "NewSlot",
                                sessionNo,
                                sessionFilter,
                                slotType

                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Lấy số thứ tự buổi học (session) của một môn học trong kỳ, dựa trên số lần xuất hiện của môn đó.
        /// Nếu đã đạt đến tổng số buổi thì quay lại 1.
        /// </summary>
        /// <param name="subjectAppearanceOrder">Dictionary lưu số lần xuất hiện của từng môn học.</param>
        /// <param name="subject">Môn học cần lấy số thứ tự buổi học.</param>
        /// <returns>Số thứ tự buổi học hiện tại của môn học.</returns>
        private int GetSessionNo(Dictionary<string, int> subjectAppearanceOrder, Subject subject)
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));

            if (!subjectAppearanceOrder.TryGetValue(subject.SubjectCode, out int currentSession))
            {
                currentSession = 1;
            }
            else
            {
                currentSession = currentSession >= subject.TotalSessions ? 1 : currentSession + 1;
            }

            subjectAppearanceOrder[subject.SubjectCode] = currentSession;
            return currentSession;
        }



        /// Sinh mã loại slot (slotTypeCode) dựa trên ngày, slot và buổi học (AM/PM).
        /// Quy tắc:
        /// - Nếu là slot 1, các ngày 2, 3, 4, 5 sẽ trả về mã tương ứng (A24, A35, A42, A53 hoặc P24, P35, P42, P53).
        /// - Nếu là slot 2, các ngày 2, 3, 4, 5 sẽ trả về mã đảo ngược với slot 1 (A42, A53, A24, A35 hoặc P42, P53, P24, P35).
        /// - Các ngày khác hoặc slot khác sẽ trả về chuỗi rỗng.
        /// </summary>
        /// <param name="day">Thứ trong tuần (1=Chủ nhật, 2=Thứ 2, ..., 7=Thứ 7).</param>
        /// <param name="slot">Slot trong buổi học (1 hoặc 2).</param>
        /// <param name="sessionFilter">Buổi học ("A" cho AM, "P" cho PM).</param>
        /// <returns>Mã loại slot (slotTypeCode) hoặc chuỗi rỗng nếu không khớp quy tắc.</returns>
        private string GetSlotTypeCode(int day, int slot, string sessionFilter)
        {
            if ((slot != 1 && slot != 2) || string.IsNullOrWhiteSpace(sessionFilter))
                return string.Empty;

            var map = slot == 1 ? Slot1Map : Slot2Map;

            return map.TryGetValue(day, out var code)
                ? $"{sessionFilter.ToUpperInvariant()}{code}"
                : string.Empty;
        }



        private int CalculateNumberOfRooms(int numberOfClass)
        {
            return (numberOfClass + 1) / 2; // Làm tròn lên, tối ưu hơn
        }

        private string GetSlotTypeForWeek(int week)
        {
            return week % 2 == 0 ? "online" : "offline";
        }


        /// <summary>
        /// Xác định chỉ số lớp (classIndex) và cấp chu kỳ (cycleLevel) dựa trên số phòng (roomNo).
        /// - Mỗi giảng viên có thể dạy tối đa 4 lớp trong một session, mỗi lớp có 2 slot.
        /// - Nếu số phòng vượt quá 4, các lớp sẽ được chia thành các chu kỳ (cycle), mỗi chu kỳ gồm 4 lớp.
        /// - classIndex: vị trí lớp trong chu kỳ (0-3).
        /// - cycleLevel: số chu kỳ hiện tại (bắt đầu từ 1).
        /// </summary>
        /// <param name="roomNo">Số phòng/lớp hiện tại (bắt đầu từ 1).</param>
        /// <returns>
        /// Tuple gồm:
        /// - classIndex: chỉ số lớp trong chu kỳ (0-based).
        /// - cycleLevel: cấp chu kỳ (bắt đầu từ 1).
        /// </returns>
        public static (int cyclePosition, int cycleLevel) MapToCycle(int roomNo)
        {
            int classIndex = 0;
            int cycleLevel = 0;

            // In weekly schedule, in one session, one lecturer can teach a maximum of 4 classes, each class has 2 slots
            // when number of class is greater than 4, we need to add other lecturers to the schedule
            // Determine the class index and cycle level based on the room number (one class per room)
            // For example:
            // 5: position = 1, cycle = 1, level = 1
            // 6: position = 2, cycle = 1, level = 1
            // 10: position = 2, cycle = 2, level = 2
            // 14: position = 2, cycle = 3, level = 3
            if (roomNo > 4)
            {
                // Find the position in the cycle (1 to 4)
                // For example: room 5, 6, 7, 8 will be in cycle 1, position 1,2,3,4 respectively
                int cyclePosition = (roomNo - 1) % 4 + 1;
                // Adjusted index for the class
                classIndex = cyclePosition - 1; // Adjusted index for the class

                // Calculate the number of cycles
                // For example: room 5, 6, 7, 8 will be in cycle 1, room 9, 10, 11, 12 will be in cycle 2
                int cycleCount = (roomNo - 1) / 4;

                // Calculate the cycle level
                cycleLevel = cycleCount + 1; // Adjusted to start from 1
            }
            else
            {
                classIndex = roomNo - 1; // Adjusted index for the class
                cycleLevel = 1;
            }

            return (classIndex, cycleLevel); // Return the adjusted class index and cycle level
        }
    }
}
