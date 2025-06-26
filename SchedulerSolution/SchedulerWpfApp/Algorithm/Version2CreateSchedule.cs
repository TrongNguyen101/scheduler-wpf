using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;

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

            // *** TỐI ƯU HIỆU SUẤT: 4 luồng chính được chạy đồng thời, không cần chờ đợi nhau ***
            var schedulingTasks = new List<Task<List<Schedule>>>
                                    {
                                        GenerateSchedulesForSessionAsync(context, ScheduleConstants.FirstAndFinalWeeks, startDate, "A"),
                                        GenerateSchedulesForSessionAsync(context, ScheduleConstants.FirstAndFinalWeeks, startDate, "P"),
                                        GenerateSchedulesForSessionAsync(context, ScheduleConstants.MidTermWeeks, startDate, "A"),
                                        GenerateSchedulesForSessionAsync(context, ScheduleConstants.MidTermWeeks, startDate, "P")
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

        private async Task<List<Schedule>> GenerateSchedulesForSessionAsync(SchedulingContext context, IEnumerable<int> weeksToProcess, DateTime startDate, string sessionFilter)
        {

            var sessionSchedules = new List<Schedule>();
            var subjectAppearanceOrder = new Dictionary<string, int>(); // Trạng thái cho mỗi luồng
            int slotStart = sessionFilter == "A" ? 1 : 3;

            // Lấy phòng và xây dựng cây song song ---
            var listRooms = await _roomService.GetNumberOfRoom(context.GroupClasses.Count);
            if (listRooms == null || !listRooms.Any()) return sessionSchedules;

            // *** TỐI ƯU CRITICAL: Chạy song song việc xây dựng cây cho tất cả các phòng ***
            var buildTreeTasks = listRooms.Select(room =>
                _treeNode.BuildTreeForRoom(room.RoomId, room.RoomName, ScheduleConstants.DefaultSlotStatus)
            ).ToList();
            var roomNodes = await Task.WhenAll(buildTreeTasks);

            // --- 4.2: Vòng lặp chính để tạo lịch ---
            for (int indexRoom = 0; indexRoom < listRooms.Count; indexRoom++)
            {
                // Thêm kiểm tra an toàn để tránh lỗi
                if (indexRoom >= context.GroupClasses.Count) continue;

                var roomNode = roomNodes[indexRoom];
                var group = context.GroupClasses[indexRoom];

                // Sử dụng Lookup đã được tiền xử lý hiệu quả
                var subjectOfClass = context.CurriculumLookup[(group.CurriculumCode, group.Term)].ToList();
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

                            string slotTypeCode = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, context.Session.ToString());
                            bool isOffline = GetSlotTypeForWeek(week, dayOfWeek, slotTypeCode);
                            string slotDeliveryType = (isOffline ? SlotDeliveryType.Offline : SlotDeliveryType.Online).ToString().ToLower();

                            // Hợp nhất logic gọi hàm GetSessionNo
                            int sessionNo = ScheduleConstants.FirstAndFinalWeeks.Contains(week)
                                ? GetSessionNoForFirstWeeksAndFinal(subjectAppearanceOrder, subject, week)
                                : GetSessionNoForWeeks(subjectAppearanceOrder, subject);

                            var (lecturerId, lecturerName) = _getLecturerForSubject.FindLecturerForSubject(subject.SubjectCode, context.LecturersTeachSubjectSession, cycleLevel);
                            string slotLabel = $"slot {slotIndex + slotStart}";

                            var schedulesItem = _treeNode.CollectSchedules(roomNode, subject.SubjectCode, currentDate, group.GroupName, slotLabel, lecturerId, lecturerName, slotTypeCode, ScheduleConstants.DefaultSlotStatus, sessionNo, context.Session.ToString(), slotDeliveryType);
                            sessionSchedules.AddRange(schedulesItem);
                        }
                    }
                }
            }
            return sessionSchedules;
        }

    }
}
