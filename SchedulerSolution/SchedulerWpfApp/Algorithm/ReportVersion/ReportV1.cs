using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;

namespace SchedulerWpfApp.Algorithm.ReportVersion
{
    public class ReportV1
    {
        private readonly ILogger<ReportV1> _logger;

        private readonly IScheduleServices _scheduleServices;
        private readonly ILecturerSubjectServices _lecturerSubjectServices;
        private readonly ICurriculumSubjectServices _curriculumSubjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly ILecturerServices _lecturerServices;
        private readonly IRoomService _roomService;

        private readonly TreeForSchedule _treeNode;
        private readonly SortSubjectsOneSession _sortSubjectsOneSession;
        private readonly CreateSlotTypeCode _createSlotTypeCode;
        private readonly LecturerAssignmentService _lecturerAssignmentService;

        private SchedulingContext _context; // Lưu trữ ngữ cảnh đã chuẩn bị

        public ReportV1(ILogger<ReportV1> logger,
                        IScheduleServices scheduleServices,
                        ILecturerSubjectServices lecturerSubjectServices,
                        ILecturerServices lecturerServices,
                        IGroupNameService groupNameService,
                        ICurriculumSubjectServices curriculumSubjectServices,
                        IRoomService roomService,
                        TreeForSchedule treeNode,
                        SortSubjectsOneSession sortSubjectsOneSession,
                        CreateSlotTypeCode createSlotTypeCode,
                        SchedulingContext context,
                        LecturerAssignmentService lecturerAssignmentService)
        {
            _logger = logger;
            _scheduleServices = scheduleServices;
            _lecturerSubjectServices = lecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameService;
            _curriculumSubjectServices = curriculumSubjectServices;

            _treeNode = treeNode;
            _sortSubjectsOneSession = sortSubjectsOneSession;
            _roomService = roomService;
            _createSlotTypeCode = createSlotTypeCode;
            _context = context;
            _lecturerAssignmentService = lecturerAssignmentService;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate, List<string> listMajorGroupA, List<string> listMajorGroupB)
        {
            try
            {
                _context = await PrepareSchedulingDataAsync();

                if (_context == null || !_context.Rooms.Any() || !_context.GroupNames.Any() || !_context.CurriculumSubjects.Any())
                {
                    _logger.LogWarning("Context or required data is missing.");
                    return new List<Schedule>(); // Trả về danh sách rỗng nếu không có phòng
                }

                //Lấy danh sách lớp theo buổi và ko phải là lớp đi OJT
                List<GroupClass> listGroupNameAm = _context.GroupNames.Where(g => g.PartOfDayInTheFirstTerm == "A" && g.TeachingMode != "OJT").ToList();
                List<GroupClass> listGroupNamePm = _context.GroupNames.Where(g => g.PartOfDayInTheFirstTerm == "P" && g.TeachingMode != "OJT").ToList();

                var roomNodesAMFirstAndFinalWeek = await BuildRoomTreeForSchedulesFullOff(listGroupNameAm.Count);
                var roomNodesPMFirstAndFinalWeek = await BuildRoomTreeForSchedulesFullOff(listGroupNamePm.Count);

                var dataContextInAmFullOff = new SchedulePartOfDayContext
                {
                    GroupNames = listGroupNameAm,
                    LecturersTeachSubjects = _context.LecturersTeachSubjects,
                    PartOfDay = ScheduleConstants.PartOfDayIsAM,
                    StartDate = startDate,
                    TreeForSchedules = roomNodesAMFirstAndFinalWeek
                };

                var dataContextInPmFullOff = new SchedulePartOfDayContext
                {
                    GroupNames = listGroupNamePm,
                    LecturersTeachSubjects = _context.LecturersTeachSubjects,
                    PartOfDay = ScheduleConstants.PartOfDayIsPM,
                    StartDate = startDate,
                    TreeForSchedules = roomNodesPMFirstAndFinalWeek
                };

                var allSchedules = new List<Schedule>();

                // *** TỐI ƯU HIỆU SUẤT: 4 luồng chính được chạy đồng thời, không cần chờ đợi nhau ***
                var schedulingTasks = new List<Task<List<Schedule>>>
                                    {
                                        GenerateSchedulesFullOffAsync(dataContextInAmFullOff),
                                        GenerateSchedulesFullOffAsync(dataContextInPmFullOff),
                                    };

                var results = await Task.WhenAll(schedulingTasks);
                allSchedules = results.SelectMany(list => list).ToList();

                _lecturerAssignmentService.AssignLecturers(_context.LecturerSubjects, allSchedules);

                return allSchedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schedules");
                return new List<Schedule>();  // Trả về danh sách rỗng nếu có lỗi xảy ra
            }
        }

        private async Task<SchedulingContext> PrepareSchedulingDataAsync()
        {
            // Khởi tạo các Task để tải dữ liệu đồng thời
            var lecturerTask = _lecturerServices.GetAllLecturerAsync();
            var roomTask = _roomService.GetAllAsync();
            var groupNameTask = _groupNameService.GetAllAsync();
            var lecturerSubjectTask = _lecturerSubjectServices.GetAllAsync();
            var curriculumSubjectTask = _curriculumSubjectServices.GetAllCurriculumSubjectAsync();
            var deleteAllSchedulesTask = _scheduleServices.DeleteAllAsync();

            // Chờ tất cả các Task hoàn thành
            await Task.WhenAll(lecturerTask, roomTask, groupNameTask, lecturerSubjectTask, curriculumSubjectTask, deleteAllSchedulesTask);

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

            var scheduleSubjectsLookup = ScheduleSubjectsLookup(curriculumLookup, listGroupName);

            return new SchedulingContext
            {
                Lecturers = listLecturer,
                LecturerRequests = new List<LecturerRequest>(),
                LecturerSubjects = lecturersSubjects,
                Rooms = listRoom,
                CurriculumSubjects = curriculumSubjects,
                GroupNames = listGroupName,
                CurriculumLookup = curriculumLookup,
                LecturersTeachSubjects = lecturersTeachSubjects,
                SchedulesSubjectsLookup = scheduleSubjectsLookup,
            };
        }

        // Phương thức để tiền xử lý các môn học
        private Dictionary<string, CurriculumSubjectWithCount[,,]> ScheduleSubjectsLookup(ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup, List<GroupClass> listGroupName)
        {
            var subjectLookup = new Dictionary<string, CurriculumSubjectWithCount[,,]>();
            List<GroupClass> class5subject = new List<GroupClass>();
            List<GroupClass> class4subject = new List<GroupClass>();
            List<GroupClass> class3subject = new List<GroupClass>();
            List<GroupClass> class2subject = new List<GroupClass>();
            List<GroupClass> class1subject = new List<GroupClass>();

            foreach (var groupName in listGroupName)
            {
                var subjectsOfClass = curriculumLookup[(groupName.CurriculumCode, groupName.Term.GetValueOrDefault())].ToList();
                //var subjectOfClass = curriculumLookup[("BIT_GD_MCD_18A", 8)].ToList();

                var listGroupNameOJT = listGroupName.Where(g => g.TeachingMode == ScheduleConstants.TechingModeIsOJT).ToList();

                var listSubjectOnOffNomalAndHalfOne = subjectsOfClass
                                                     .Where(s => s.TeachingMode == ScheduleConstants.TechingModeIsOnOff && (s.PartOfTerm.Contains("H1") || s.PartOfTerm == "All") && !s.SubjectCode.Contains("GRA"))
                                                     .ToList();

                if (listSubjectOnOffNomalAndHalfOne.Count == 2)
                {
                    class2subject.Add(groupName);
                    continue; // Không cần xử lý thêm, chỉ cần ghi nhận lớp có 2 môn học
                }
                else
                if (listSubjectOnOffNomalAndHalfOne.Count == 3)
                {
                    class3subject.Add(groupName);
                    var curriculumTemp = new CurriculumSubject();
                    listSubjectOnOffNomalAndHalfOne.Add(curriculumTemp); // Thêm một môn học tạm thời để đảm bảo có đủ 4 môn

                    var sortedFourSubjects = _sortSubjectsOneSession.SortSubjectFourClassFlexibleSubject(listSubjectOnOffNomalAndHalfOne);

                    var subjectOffClassDifferentTime = subjectsOfClass.FirstOrDefault(s => s.TeachingMode == "OFF");

                    subjectLookup[groupName.GroupName] = sortedFourSubjects;
                }
                else if (listSubjectOnOffNomalAndHalfOne.Count == 4)
                {
                    class4subject.Add(groupName);
                    var sortedFourSubjects = _sortSubjectsOneSession.SortSubjectFourClassFlexibleSubject(listSubjectOnOffNomalAndHalfOne);
                    subjectLookup[groupName.GroupName] = sortedFourSubjects;
                }
                else if (listSubjectOnOffNomalAndHalfOne.Count == 5)
                {
                    class5subject.Add(groupName);
                    var sortedFiveSubjects = _sortSubjectsOneSession.SortFiveSubjectForClass(listSubjectOnOffNomalAndHalfOne);
                    subjectLookup[groupName.GroupName] = sortedFiveSubjects;
                }
            }
            return subjectLookup;
        }

        private async Task<List<TreeForSchedule>> BuildRoomTreeForSchedulesFullOff(int numberOfRooms)
        {
            var listRoomNodes = new List<TreeForSchedule>();
            var listRooms = await _roomService.GetNumberOfRoom(numberOfRooms);
            if (listRooms == null || !listRooms.Any()) return listRoomNodes;

            // Chạy song song việc xây dựng cây cho tất cả các phòng
            var buildTreeTasks = listRooms.Select(room =>
                _treeNode.BuildTreeForRoom(room.RoomId, room.RoomName)
            ).ToList();
            var roomNodes = await Task.WhenAll(buildTreeTasks);
            listRoomNodes = roomNodes.ToList(); // Chuyển đổi kết quả thành List

            return listRoomNodes;
        }

        private async Task<List<Schedule>> GenerateSchedulesFullOffAsync(SchedulePartOfDayContext dataContextInPartOfDay)
        {
            try
            {
                var sessionSchedules = new List<Schedule>();  // Tạo danh sách với dung lượng đủ
                var subjectAppearanceOrder = new Dictionary<string, int>(); // Trạng thái cho mỗi luồng
                int slotStart = dataContextInPartOfDay.PartOfDay == ScheduleConstants.PartOfDayIsAM ? ScheduleConstants.NewSlotStartTimeAM : ScheduleConstants.NewSlotStartTimePM;

                // Tiền xử lý dữ liệu

                string statusSlot = ScheduleConstants.StatusSlotIsOffline; // Mặc định là Offline

                // Sử dụng Parallel để tối ưu việc xử lý đồng thời các phòng học
                var tasks = dataContextInPartOfDay.TreeForSchedules.Select((roomNode, indexRoom) => Task.Run(() =>
                    GenerateRoomSchedulesFullOff(indexRoom, dataContextInPartOfDay, slotStart, subjectAppearanceOrder, statusSlot)
                ));

                var results = await Task.WhenAll(tasks);
                foreach (var result in results)
                {
                    sessionSchedules.AddRange(result);
                }

                return sessionSchedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schedules");
                return new List<Schedule>();  // Trả về danh sách rỗng nếu có lỗi
            }
        }

        // Phương thức để tạo lịch cho từng phòng học
        private List<Schedule> GenerateRoomSchedulesFullOff(
            int indexRoom,
            SchedulePartOfDayContext dataContextInPartOfDay,
            int slotStart,
            Dictionary<string, int> subjectAppearanceOrder,
            string statusSlot)
        {
            var sessionSchedules = new List<Schedule>();
            var roomNode = dataContextInPartOfDay.TreeForSchedules[indexRoom];
            var groupClass = dataContextInPartOfDay.GroupNames[indexRoom];

            var scheduleSubjectForClassForRoom = _context.SchedulesSubjectsLookup.ContainsKey(groupClass.GroupName)
                ? _context.SchedulesSubjectsLookup[groupClass.GroupName]
                : null;

            var classIndex = 0;
            var cycleLevel = 0;

            if (scheduleSubjectForClassForRoom?.GetLength(1) == 2)
            {
                classIndex = MapToCycleTwoClasses(indexRoom + 1);
            }
            else
            {
                classIndex = MapToCycleFourClasses(indexRoom + 1);
            }

            int[] weeksToProcess = new int[] { 1 };


            foreach (int week in weeksToProcess)
            {
                for (int dayOfWeek = 1; dayOfWeek <= ScheduleConstants.DaysInWeek; dayOfWeek++)
                {
                    DateTime currentDate = dataContextInPartOfDay.StartDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));

                    for (int slotIndex = 0; slotIndex < ScheduleConstants.SlotsPerSession; slotIndex++)
                    {
                        if (scheduleSubjectForClassForRoom == null) continue; // Nếu không có lịch môn học cho lớp này thì bỏ qua

                        var subjectSorted = scheduleSubjectForClassForRoom[dayOfWeek, classIndex, slotIndex];

                        if (subjectSorted == null) continue;

                        if (string.IsNullOrEmpty(subjectSorted.Subject.CurriculumCode)) continue;

                        string typeSlot = subjectSorted.Subject.TeachingMode == ScheduleConstants.TechingModeIsCoursera ? ScheduleConstants.TypeSlotIsOld : ScheduleConstants.TypeSlotIsNew;
                        if (subjectSorted.Subject.TeachingMode == ScheduleConstants.TechingModeIsCoursera)
                        {
                            statusSlot = ScheduleConstants.StatusSlotIsOnline;  // Coursera luôn là Online
                        }

                        string slotTypeCode = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, dataContextInPartOfDay.PartOfDay, subjectSorted.Subject.TeachingMode);

                        int sessionNo = 0;

                        // tính số thứ tự slot dựa vào tuần và mã đã đánh số theo lịch môn học đã xếp
                        if (week == 1)
                        {
                            sessionNo = subjectSorted.Count;
                        }
                        else
                        {
                            if (subjectSorted.Subject.TotalSlots == ScheduleConstants.TotalSlotsNomal)
                            {
                                sessionNo = subjectSorted.Count == 1 ? (2 * week - 1) : (2 * week); // Tuần 1 là slot thứ 1, tuần 2 là slot thứ 3, tuần 3 là slot thứ 5, v.v.
                            }
                            else
                            {
                                sessionNo = 0;
                            }
                        }
                        int slotLabel = slotIndex + slotStart;

                        var schedulesItem = _treeNode.CollectSchedules(roomNode, subjectSorted.Subject.SubjectCode, currentDate, groupClass.GroupName, slotLabel, null, null, null, slotTypeCode, typeSlot, sessionNo, dataContextInPartOfDay.PartOfDay, statusSlot);
                        sessionSchedules.AddRange(schedulesItem);
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
        private int MapToCycleFourClasses(int roomNo)
        {
            int classIndex = 0;

            // In weekly schedule, with class have 4 subject in one session, one lecturer can teach a maximum of 4 classes, each class has 2 slots
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

            }
            else
            {
                classIndex = roomNo - 1; // Adjusted index for the class
            }

            return classIndex; // Return the adjusted class index and cycle level
        }

        private int MapToCycleTwoClasses(int roomNo)
        {
            int classIndex = 0;

            // In weekly schedule, with class have 5 subject in one session, one lecturer can teach a maximum of 2 classes, each class has 2 slots
            // when number of class is greater than 2, we need to add other lecturers to the schedule
            // Determine the class index and cycle level based on the room number (one class per room)
            // For example:
            // 3: position = 1, cycle = 1, level = 1
            // 4: position = 2, cycle = 1, level = 1
            // 5: position = 2, cycle = 2, level = 2
            // 7: position = 2, cycle = 3, level = 3
            if (roomNo > 2)
            {
                // Find the position in the cycle (1 to 2)
                // For example: room 3, 4 will be in cycle 1, position 1,2 respectively
                int cyclePosition = (roomNo - 1) % 2 + 1;
                // Adjusted index for the class
                classIndex = cyclePosition - 1; // Adjusted index for the class

                // Calculate the number of cycles
                // For example: room 3, 4 will be in cycle 1, room 5, 6 will be in cycle 2
                int cycleCount = (roomNo - 1) / 2;
            }
            else
            {
                classIndex = roomNo - 1; // Adjusted index for the class
            }

            return classIndex; // Return the adjusted class index and cycle level
        }
    }
}
