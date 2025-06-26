using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using System.Windows.Controls;

namespace SchedulerWpfApp.Algorithm
{
    public class Version2CreateSchedule
    {
        private readonly IScheduleServices _scheduleServices;
        private readonly ILecturerSubjectServices _lecturerSubjectServices;
        private readonly ICurriculumSubjectServices _curriculumSubjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly ILecturerServices _lecturerServices;
        private readonly IRoomService _roomService;

        private readonly TreeForSchedule _treeNode;
        private readonly SortSubjectsOneSession _sortSubjectsOneSession;
        private readonly GetLecturerForSubject _getLecturerForSubject;
        private readonly CreateSlotTypeCode _createSlotTypeCode;

        public Version2CreateSchedule(IScheduleServices scheduleServices,
                                      ILecturerSubjectServices lecturerSubjectServices,
                                      ILecturerServices lecturerServices,
                                      IGroupNameService groupNameService,
                                      ICurriculumSubjectServices curriculumSubjectServices,
                                      IRoomService roomService,
                                      TreeForSchedule treeNode,
                                      GetLecturerForSubject getLecturerForSubject,
                                      SortSubjectsOneSession sortSubjectsOneSession,
                                      CreateSlotTypeCode createSlotTypeCode)
        {
            _scheduleServices = scheduleServices;
            _lecturerSubjectServices = lecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameService;
            _curriculumSubjectServices = curriculumSubjectServices;

            _treeNode = treeNode;
            _sortSubjectsOneSession = sortSubjectsOneSession;
            _getLecturerForSubject = getLecturerForSubject;
            _roomService = roomService;
            _createSlotTypeCode = createSlotTypeCode;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            var listMajorA = new List<string> { "FN", "HM", "MC", "BA", "TM" };
            var listMajorB = new List<string> { "AI", "SE", "AI", "JL", "KR", "EL" };
            var listMajorFullOff = new List<string> { "GD" };

            var context = await PrepareSchedulingDataAsync();

            if (context == null || !context.Rooms.Any() || !context.GroupNames.Any() || !context.CurriculumSubjects.Any())
            {
                return new List<Schedule>(); // Trả về danh sách rỗng nếu không có phòng
            }

            var (listGroupNameAm, listGroupNamePm) = BalancedSplitWithGreedySwap(context.GroupNames);

            var lecturersAM = _getLecturerForSubject.FilterLecturerInSession(context.LecturersTeachSubjects, context.LecturerRequests, "AM");
            var lecturersPM = _getLecturerForSubject.FilterLecturerInSession(context.LecturersTeachSubjects, context.LecturerRequests, "PM");

            var contextAm = new SchedulingContext
            {
                Lecturers = context.Lecturers,
                Rooms = context.Rooms,
                CurriculumSubjects = context.CurriculumSubjects,
                GroupNames = listGroupNameAm,
                CurriculumLookup = context.CurriculumLookup,
                LecturersTeachSubjects = lecturersAM,
                StartDate = startDate,
                PartOfDay = "A"
            };

            var contextPm = new SchedulingContext
            {
                Lecturers = context.Lecturers,
                Rooms = context.Rooms,
                CurriculumSubjects = context.CurriculumSubjects,
                GroupNames = listGroupNamePm,
                CurriculumLookup = context.CurriculumLookup,
                LecturersTeachSubjects = lecturersPM,
                StartDate = startDate,
                PartOfDay = "P"
            };

            // *** TỐI ƯU HIỆU SUẤT: 4 luồng chính được chạy đồng thời, không cần chờ đợi nhau ***
            var schedulingTasks = new List<Task<List<Schedule>>>
                                    {
                                        GenerateSchedulesForSessionAsync(contextAm, ScheduleConstants.FirstAndFinalWeeks, listMajorA, listMajorB),
                                        GenerateSchedulesForSessionAsync(contextPm, ScheduleConstants.FirstAndFinalWeeks, listMajorA, listMajorB),
                                        GenerateSchedulesForSessionAsync(contextAm, ScheduleConstants.MidTermWeeks, listMajorA, listMajorB),
                                        GenerateSchedulesForSessionAsync(contextPm, ScheduleConstants.MidTermWeeks, listMajorA, listMajorB)
                                    };

            var results = await Task.WhenAll(schedulingTasks);
            var allSchedules = results.SelectMany(list => list).ToList();

            return allSchedules;
        }

        private async Task<SchedulingContext> PrepareSchedulingDataAsync()
        {
            // Khởi tạo các Task để tải dữ liệu đồng thời
            var lecturerTask = _lecturerServices.GetAllLecturerAsync();
            var roomTask = _roomService.GetAllAsync();
            var groupNameTask = _groupNameService.GetAllAsync();
            var lecturerSubjectTask = _lecturerSubjectServices.GetAllAsync();
            var curriculumSubjectTask = _curriculumSubjectServices.GetAllCurriculumSubjectAsync();

            // Chờ tất cả các Task hoàn thành
            await Task.WhenAll(lecturerTask, roomTask, groupNameTask, lecturerSubjectTask, curriculumSubjectTask);

            // Lấy kết quả từ các Task
            var listLecturer = await lecturerTask;
            var listRoom = await roomTask;
            var listGroupName = await groupNameTask;
            var lecturersSubjects = await lecturerSubjectTask;
            var curriculumSubjects = await curriculumSubjectTask;

            var lecturersTeachSubjects = lecturersSubjects.GroupBy(l => l.SubjectCode).ToDictionary(g => g.Key, g => g.ToList());

            // *** TỐI ƯU HIỆU SUẤT: Tiền xử lý dữ liệu để tra cứu nhanh (O(1)) ***
            // Chuyển List thành Lookup để tìm kiếm môn học không cần duyệt lại toàn bộ danh sách.
            var curriculumLookup = curriculumSubjects.ToLookup(s => (s.CurriculumCode, s.TermNo));

            return new SchedulingContext
            {
                Lecturers = listLecturer,
                LecturerRequests = new List<LecturerRequest>(),
                Rooms = listRoom,
                CurriculumSubjects = curriculumSubjects,
                GroupNames = listGroupName,
                CurriculumLookup = curriculumLookup,
                LecturersTeachSubjects = lecturersTeachSubjects,
            };
        }

        /// <summary>
        /// Chia danh sách các lớp (GroupName) thành hai nhóm sáng và chiều một cách cân bằng.
        /// - Nhóm theo chuyên ngành (Major), sau đó chia mỗi nhóm thành hai nửa (sáng, chiều).
        /// - Sử dụng thuật toán greedy để phân phối các nửa vào hai nhóm tổng thể sao cho số lượng lớp giữa hai nhóm cân bằng nhất.
        /// - Nếu tổng số lớp của nhóm sáng nhỏ hơn hoặc bằng nhóm chiều thì thêm nửa sáng vào nhóm sáng, nửa chiều vào nhóm chiều.
        /// - Ngược lại, đảo ngược phân phối để cân bằng số lượng lớp giữa hai nhóm.
        /// </summary>
        /// <param name="allGroupNames">Danh sách tất cả các lớp cần chia.</param>
        /// <returns>
        /// Tuple gồm:
        /// - morningGroups: danh sách lớp học buổi sáng.
        /// - afternoonGroups: danh sách lớp học buổi chiều.
        /// </returns>
        public (List<GroupClass> morningGroups, List<GroupClass> afternoonGroups) BalancedSplitWithGreedySwap(List<GroupClass> allGroupNames)
        {
            var morningGroups = new List<GroupClass>();
            var afternoonGroups = new List<GroupClass>();

            var groupedByCurriculum = allGroupNames
                .GroupBy(g => g.CurriculumCode)
                .Select(g => new
                {
                    CurriculumCode = g.Key,
                    Group = g.ToList()
                }).ToList();

            // Tạo danh sách các cặp (nửa sáng, nửa chiều)
            var splitPairs = new List<(List<GroupClass> MorningHalf, List<GroupClass> AfternoonHalf)>();

            foreach (var item in groupedByCurriculum)
            {
                int count = item.Group.Count;
                int half = count / 2;
                int extra = count % 2;

                var morningHalf = item.Group.Take(half + extra).ToList();
                var afternoonHalf = item.Group.Skip(half + extra).ToList();

                splitPairs.Add((morningHalf, afternoonHalf));
            }

            // Greedy phân phối để cân bằng
            int totalMorning = 0;
            int totalAfternoon = 0;

            foreach (var (morningHalf, afternoonHalf) in splitPairs)
            {
                int morningSize = morningHalf.Count;
                int afternoonSize = afternoonHalf.Count;

                if (totalMorning <= totalAfternoon)
                {
                    morningGroups.AddRange(morningHalf);
                    afternoonGroups.AddRange(afternoonHalf);
                    totalMorning += morningSize;
                    totalAfternoon += afternoonSize;
                }
                else
                {
                    // Đảo ngược phân phối
                    morningGroups.AddRange(afternoonHalf);
                    afternoonGroups.AddRange(morningHalf);
                    totalMorning += afternoonSize;
                    totalAfternoon += morningSize;
                }
            }

            return (morningGroups, afternoonGroups);
        }

        private async Task<List<Schedule>> GenerateSchedulesForSessionAsync(SchedulingContext context, IEnumerable<int> weeksToProcess, List<string> listMajorA, List<string> listMajorB)
        {

            var sessionSchedules = new List<Schedule>();
            var subjectAppearanceOrder = new Dictionary<string, int>(); // Trạng thái cho mỗi luồng
            int slotStart = context.PartOfDay == "A" ? 1 : 3;

            List<TreeForSchedule> roomNodes = new List<TreeForSchedule>();

            if (weeksToProcess.Count() > 2)
            {
                var listGroupNameAlternatingA = context.GroupNames.Where(group => listMajorA.Contains(group.Major)).ToList();
                var listGroupNameAlternatingB = context.GroupNames.Where(group => listMajorB.Contains(group.Major)).ToList();
                int numberOfRoomsForAllClass = Math.Max(listGroupNameAlternatingA.Count, listGroupNameAlternatingB.Count);
                var listRooms = await _roomService.GetNumberOfRoom(numberOfRoomsForAllClass);
                if (listRooms == null || !listRooms.Any()) return sessionSchedules;
                var typeOfSlot = ScheduleConstants.DefaultSlotStatus ? "NewSlot" : "OldSlot";

                // *** TỐI ƯU CRITICAL: Chạy song song việc xây dựng cây cho tất cả các phòng ***
                var buildTreeTasks = listRooms.Select(room =>
                    _treeNode.BuildTreeForRoom(room.RoomId, room.RoomName, typeOfSlot)
                ).ToList();

                // Get the result of the tasks, which will be a List of TreeForSchedule
                var result = await Task.WhenAll(buildTreeTasks);

                // Convert the result to a List and assign it to roomNodes
                roomNodes = result.ToList();

            }
            else
            {
                // Lấy phòng và xây dựng cây song song
                var listRooms = await _roomService.GetNumberOfRoom(context.GroupNames.Count());
                if (listRooms == null || !listRooms.Any()) return sessionSchedules;

                var typeOfSlot = ScheduleConstants.DefaultSlotStatus ? "NewSlot" : "OldSlot";

                // *** TỐI ƯU CRITICAL: Chạy song song việc xây dựng cây cho tất cả các phòng ***
                var buildTreeTasks = listRooms.Select(room =>
                    _treeNode.BuildTreeForRoom(room.RoomId, room.RoomName, typeOfSlot)
                ).ToList();

                // Get the result of the tasks, which will be a List of TreeForSchedule
                var result = await Task.WhenAll(buildTreeTasks);

                // Convert the result to a List and assign it to roomNodes
                roomNodes = result.ToList();
            }

            // --- 4.2: Vòng lặp chính để tạo lịch ---
            for (int indexRoom = 0; indexRoom < roomNodes.Count; indexRoom++)
            {
                // Thêm kiểm tra an toàn để tránh lỗi
                if (indexRoom >= context.GroupNames.Count) continue;

                var roomNode = roomNodes[indexRoom];
                var group = context.GroupNames[indexRoom];

                // Sử dụng Lookup đã được tiền xử lý hiệu quả
                var subjectOfClass = context.CurriculumLookup[(group.CurriculumCode, group.Term.GetValueOrDefault())].ToList();
                var scheduleSubjectForClass = _sortSubjectsOneSession.SortSubjectFourClass(subjectOfClass);
                var (classIndex, cycleLevel) = MapToCycle(indexRoom + 1);

                foreach (int week in weeksToProcess)
                {
                    for (int dayOfWeek = 1; dayOfWeek <= ScheduleConstants.DaysInWeek; dayOfWeek++)
                    {
                        DateTime currentDate = context.StartDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));

                        for (int slotIndex = 0; slotIndex < ScheduleConstants.SlotsPerSession; slotIndex++)
                        {
                            var subject = scheduleSubjectForClass[dayOfWeek, classIndex, slotIndex];
                            if (subject == null) continue;

                            string slotTypeCode = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, context.PartOfDay);
                            bool isOffline = GetSlotTypeForWeek(week, dayOfWeek, slotTypeCode);
                            string slotDeliveryType = (isOffline ? "offline" : "Online").ToString().ToLower();

                            // Hợp nhất logic gọi hàm GetSessionNo
                            int sessionNo = ScheduleConstants.FirstAndFinalWeeks.Contains(week)
                                ? GetSessionNoForFirstWeeksAndFinal(subjectAppearanceOrder, subject, week)
                                : GetSessionNoForWeeks(subjectAppearanceOrder, subject);

                            var (lecturerId, lecturerName) = _getLecturerForSubject.FindLecturerForSubject(subject.SubjectCode, context.LecturersTeachSubjects, cycleLevel);
                            string slotLabel = $"slot {slotIndex + slotStart}";

                            var schedulesItem = _treeNode.CollectSchedules(roomNode, subject.SubjectCode, currentDate, group.GroupName, slotLabel, lecturerId, lecturerName, slotTypeCode, "NewSlot", sessionNo, context.PartOfDay, slotDeliveryType);
                            sessionSchedules.AddRange(schedulesItem);
                        }
                    }
                }
            }
            return sessionSchedules;
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

        private bool GetSlotTypeForWeek(int week, int dayOfWeek, string slotTypeCode)
        {
            // online tương ứng với false
            // offline tương ứng với true

            // Nếu là slot học trực tuyến theo mã
            if (slotTypeCode == "AC" || slotTypeCode == "PC")
                return false;

            // Nếu là tuần đầu hoặc tuần cuối (tuần 1, 10): luôn học offline
            if (week == 1 || week == 10)
                return true;

            // Với các tuần còn lại:
            bool isEvenWeek = week % 2 == 0;
            bool isEvenDay = dayOfWeek % 2 == 0;

            // Nếu tuần chẵn: ngày chẵn online, lẻ offline
            // Nếu tuần lẻ: ngày chẵn offline, lẻ online
            return isEvenWeek == isEvenDay ? false : true;
        }

        /// <summary>
        /// Lấy số thứ tự buổi học (session) của một môn học trong tuần đầu và tuần cuối của kỳ, dựa trên số lần xuất hiện của môn đó.
        /// Nếu đã đạt đến tổng số buổi thì quay lại 1.
        /// </summary>
        /// <param name="subjectAppearanceOrder">Dictionary lưu số lần xuất hiện của từng môn học.</param>
        /// <param name="curriculumSubject">Môn học cần lấy số thứ tự buổi học.</param>
        /// <returns>Số thứ tự buổi học hiện tại của môn học.</returns>
        private int GetSessionNoForFirstWeeksAndFinal(Dictionary<string, int> subjectAppearanceOrder, CurriculumSubject curriculumSubject, int week)
        {
            if (curriculumSubject == null)
                throw new ArgumentNullException(nameof(curriculumSubject));


            if (!subjectAppearanceOrder.TryGetValue(curriculumSubject.SubjectCode, out int currentSession))
            {
                currentSession = 1;
            }
            else
            {
                if (week == 1) // Tuần cuối
                {
                    currentSession += 1;
                }
                else
                {
                    if (currentSession == 19 || currentSession == 9)
                    {
                        currentSession += 1;
                    }
                    else
                    {
                        currentSession = curriculumSubject.TotalSlots == 20 ? 19 : 9;
                    }
                }
            }

            subjectAppearanceOrder[curriculumSubject.SubjectCode] = currentSession;
            return currentSession;
        }


        /// <summary>
        /// Lấy số thứ tự buổi học (session) của một môn học trong kỳ, dựa trên số lần xuất hiện của môn đó.
        /// Nếu đã đạt đến tổng số buổi thì quay lại 1.
        /// </summary>
        /// <param name="subjectAppearanceOrder">Dictionary lưu số lần xuất hiện của từng môn học.</param>
        /// <param name="curriculumSubject">Môn học cần lấy số thứ tự buổi học.</param>
        /// <returns>Số thứ tự buổi học hiện tại của môn học.</returns>
        private int GetSessionNoForWeeks(Dictionary<string, int> subjectAppearanceOrder, CurriculumSubject curriculumSubject)
        {
            if (curriculumSubject == null)
                throw new ArgumentNullException(nameof(curriculumSubject));


            if (!subjectAppearanceOrder.TryGetValue(curriculumSubject.SubjectCode, out int currentSession))
            {
                currentSession = 3;
            }
            else
            {
                currentSession = currentSession >= 18 ? 3 : currentSession + 1;
            }

            subjectAppearanceOrder[curriculumSubject.SubjectCode] = currentSession;
            return currentSession;
        }
    }
}
