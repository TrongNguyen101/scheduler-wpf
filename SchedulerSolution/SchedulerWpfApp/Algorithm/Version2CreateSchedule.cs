using Microsoft.Extensions.Logging;
using SchedulerWpfApp.Algorithm.CommonSubject;
using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using System.Collections.Generic;

namespace SchedulerWpfApp.Algorithm
{
    public class Version2CreateSchedule
    {
        private readonly ILogger<Version2CreateSchedule> _logger;

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
        private readonly CreateScheduleCommonSubject2 _createScheduleCommonSubject2;
        private readonly LecturerAssignmentService _lecturerAssignmentService;
        private readonly ScheduleCommonSubjectVersion3 _scheduleCommonSubjectVersion3;

        private SchedulingContext _context; // Lưu trữ ngữ cảnh đã chuẩn bị

        public Version2CreateSchedule(ILogger<Version2CreateSchedule> logger,
                                      IScheduleServices scheduleServices,
                                      ILecturerSubjectServices lecturerSubjectServices,
                                      ILecturerServices lecturerServices,
                                      IGroupNameService groupNameService,
                                      ICurriculumSubjectServices curriculumSubjectServices,
                                      IRoomService roomService,
                                      TreeForSchedule treeNode,
                                      GetLecturerForSubject getLecturerForSubject,
                                      SortSubjectsOneSession sortSubjectsOneSession,
                                      CreateSlotTypeCode createSlotTypeCode,
                                      SchedulingContext context,
                                      CreateScheduleCommonSubject2 createScheduleCommonSubject2,
                                      LecturerAssignmentService lecturerAssignmentService,
                                      ScheduleCommonSubjectVersion3 scheduleCommonSubjectVersion3)
        {
            _logger = logger;
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
            _context = context;
            _createScheduleCommonSubject2 = createScheduleCommonSubject2;
            _lecturerAssignmentService = lecturerAssignmentService;
            _scheduleCommonSubjectVersion3 = scheduleCommonSubjectVersion3;
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
                // phân chia lớp học sáng chiều
                //var (listGroupNameAm, listGroupNamePm) = BalancedSplitWithGreedySwap(_context.GroupNames);

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

                var (roomNodesAMOnOff, listGroupNameAlternatingAmA, listGroupNameAlternatingAmB) = await BuildRoomTreeForSchedulesOnOffAlternative(listGroupNameAm, listMajorGroupA, listMajorGroupB);
                var (roomNodesPMOnOff, listGroupNameAlternatingPmA, listGroupNameAlternatingPmB) = await BuildRoomTreeForSchedulesOnOffAlternative(listGroupNamePm, listMajorGroupA, listMajorGroupB);

                var dataContextInAmOnOff = new SchedulePartOfDayContext
                {
                    LecturersTeachSubjects = _context.LecturersTeachSubjects,
                    PartOfDay = ScheduleConstants.PartOfDayIsAM,
                    StartDate = startDate,
                    TreeForSchedules = roomNodesAMOnOff
                };

                var dataContextInPmOnOff = new SchedulePartOfDayContext
                {
                    LecturersTeachSubjects = _context.LecturersTeachSubjects,
                    PartOfDay = ScheduleConstants.PartOfDayIsPM,
                    StartDate = startDate,
                    TreeForSchedules = roomNodesPMOnOff
                };

                var allSchedules = new List<Schedule>();

                // Test: lấy các lớp kỳ 9 để kiểm tra
                //var listGroupNameTerm9 = _context.GroupNames.Where(g => g.Term == 9).ToList();
                var allScheduleForCommonSubject = ScheduleForCommonSubject(_context.GroupNames);


                // *** TỐI ƯU HIỆU SUẤT: 4 luồng chính được chạy đồng thời, không cần chờ đợi nhau ***
                var schedulingTasks = new List<Task<List<Schedule>>>
                                    {
                                        GenerateSchedulesFullOffAsync(dataContextInAmFullOff),
                                        GenerateSchedulesFullOffAsync(dataContextInPmFullOff),
                                        //GenerateSchedulesOnOffAsync(dataContextInAmOnOff, listGroupNameAlternatingAmA, listGroupNameAlternatingAmB),
                                        //GenerateSchedulesOnOffAsync(dataContextInPmOnOff, listGroupNameAlternatingPmA, listGroupNameAlternatingPmB)
                                    };

                //var results = await Task.WhenAll(schedulingTasks);
                //allSchedules = results.SelectMany(list => list).ToList();

                //_lecturerAssignmentService.AssignLecturers(_context.LecturerSubjects, allSchedules);

                allSchedules.AddRange(allScheduleForCommonSubject);

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

            var scheduleSubjectsLookup = ScheduleSubjectsLookup(curriculumLookup, listGroupName, lecturersSubjects);

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

        private List<Schedule> ScheduleForCommonSubject(List<GroupClass> GroupNames)
        {
            //var schedules = new List<Schedule>();

            //var conclickSchedule = new List<string>();

            var lecturersTeachCommonSubjects = _context.LecturerSubjects.Where(lecturer => lecturer.Major == "Common" && lecturer.Term == 9).ToList();

            (List<Schedule> schedules, List<string> conclickSchedule) = _scheduleCommonSubjectVersion3.GenerateSchedule(GroupNames, _context.LecturerSubjects, _context.CurriculumSubjects);

            return schedules;
        }

        // Phương thức để tiền xử lý các môn học
        private Dictionary<string, CurriculumSubjectWithCount[,,]> ScheduleSubjectsLookup(ILookup<(string CurriculumCode, int TermNo), CurriculumSubject> curriculumLookup, List<GroupClass> listGroupName, List<LecturerSubject> lecturerSubjects)
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
                    if (groupName.Term == 9)
                    {
                        class2subject.Add(groupName);
                    }
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

        private async Task<(List<TreeForSchedule> listRoomNodes,
                            List<GroupClass> listGroupNameAlternatingA,
                            List<GroupClass> listGroupNameAlternatingB)>
                                                                        BuildRoomTreeForSchedulesOnOffAlternative(
                                                                                                                List<GroupClass> listGroupName,
                                                                                                                List<string> listMajorA,
                                                                                                                List<string> listMajorB)
        {
            // Lọc các nhóm lớp thuộc MajorA và MajorB
            var listGroupNameAlternatingA = listGroupName.Where(group => listMajorA.Contains(group.Major));
            var listGroupNameAlternatingB = listGroupName.Where(group => listMajorB.Contains(group.Major));

            // Số lượng phòng cần thiết
            int numberOfRoomsForAllClass = Math.Max(listGroupNameAlternatingA.Count(), listGroupNameAlternatingB.Count());

            // Lấy danh sách phòng từ dịch vụ
            var listRooms = await _roomService.GetNumberOfRoom(numberOfRoomsForAllClass);
            if (listRooms?.Any() != true)
                return (new List<TreeForSchedule>(), listGroupNameAlternatingA.ToList(), listGroupNameAlternatingB.ToList());

            // Chạy song song việc xây dựng cây cho tất cả các phòng
            var buildTreeTasks = listRooms.Select(room =>
                _treeNode.BuildTreeForRoom(room.RoomId, room.RoomName)
            );

            // Lấy kết quả của các tác vụ song song
            var result = await Task.WhenAll(buildTreeTasks);

            // Trả về kết quả
            return (result.ToList(), listGroupNameAlternatingA.ToList(), listGroupNameAlternatingB.ToList());
        }

        public async Task<List<Schedule>> GenerateSchedulesOnOffAsync(SchedulePartOfDayContext dataContextInPartOfDay,
                                                                        List<GroupClass> listGroupNameAlternatingA,
                                                                        List<GroupClass> listGroupNameAlternatingB)
        {
            try
            {
                var sessionSchedules = new List<Schedule>();  // Tạo danh sách với dung lượng đủ
                var subjectAppearanceOrder = new Dictionary<string, int>(); // Trạng thái cho mỗi luồng

                // Tiền xử lý dữ liệu

                // Sử dụng Parallel để tối ưu việc xử lý đồng thời các phòng học
                var tasks = dataContextInPartOfDay.TreeForSchedules.Select((roomNode, indexRoom) => Task.Run(() =>
                    GenerateRoomSchedulesOnlOff(indexRoom, dataContextInPartOfDay, listGroupNameAlternatingA, listGroupNameAlternatingB, ScheduleConstants.MidTermWeeks, subjectAppearanceOrder)
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

        private List<Schedule> GenerateRoomSchedulesOnlOff(
            int indexRoom,
            SchedulePartOfDayContext dataContextInPartOfDay,
            List<GroupClass> listGroupNameAlternatingA,
            List<GroupClass> listGroupNameAlternatingB,
            IEnumerable<int> weeksToProcess,
            Dictionary<string, int> subjectAppearanceOrder)
        {
            int slotStart = dataContextInPartOfDay.PartOfDay == ScheduleConstants.PartOfDayIsAM ? ScheduleConstants.NewSlotStartTimeAM : ScheduleConstants.NewSlotStartTimePM;

            var sessionSchedules = new List<Schedule>();
            var roomNode = dataContextInPartOfDay.TreeForSchedules[indexRoom];

            // Try to get the group classes using ElementAtOrDefault to avoid manual null checks
            GroupClass groupClassA = listGroupNameAlternatingA.ElementAtOrDefault(indexRoom);
            GroupClass groupClassB = listGroupNameAlternatingB.ElementAtOrDefault(indexRoom);

            if (groupClassA == null && groupClassB == null)
            {
                _logger.LogWarning($"No group classes found for room index {indexRoom}");
                return sessionSchedules; // Trả về danh sách rỗng nếu không có lớp học
            }
            else if (groupClassA != null && groupClassB == null)
            {

            }
            else if (groupClassA == null && groupClassB != null)
            {

            }
            else
            {

            }

            string groupNameA = groupClassA == null ? "" : groupClassA.GroupName;
            string groupNameB = groupClassB == null ? "" : groupClassB.GroupName;

            // Kiểm tra groupNameA trong _context.SchedulesFourSubjectsLookup
            var scheduleSubjectOfGroupNameAForRoom = _context.SchedulesSubjectsLookup.ContainsKey(groupNameA)
                ? _context.SchedulesSubjectsLookup[groupNameA]
                : null;

            // Kiểm tra groupNameB trong _context.SchedulesFourSubjectsLookup
            var scheduleSubjectOfGroupNameBForRoom = _context.SchedulesSubjectsLookup.ContainsKey(groupNameB)
                ? _context.SchedulesSubjectsLookup[groupNameB]
                : null;

            var classIndexA = 0;
            var cycleLevelA = 0;
            var classIndexB = 0;
            var cycleLevelB = 0;

            if (scheduleSubjectOfGroupNameAForRoom?.GetLength(1) == 2)
            {
                classIndexA = MapToCycleTwoClasses(indexRoom + 1);
            }
            else
            {
                classIndexA = MapToCycleFourClasses(indexRoom + 1);
            }

            if (scheduleSubjectOfGroupNameBForRoom?.GetLength(1) == 2)
            {
                classIndexB = MapToCycleTwoClasses(indexRoom + 1);
            }
            else
            {
                classIndexB = MapToCycleFourClasses(indexRoom + 1);
            }

            foreach (int week in weeksToProcess)
            {
                for (int dayOfWeek = 1; dayOfWeek <= ScheduleConstants.DaysInWeek; dayOfWeek++)
                {
                    DateTime currentDate = dataContextInPartOfDay.StartDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));

                    for (int slotIndex = 0; slotIndex < ScheduleConstants.SlotsPerSession; slotIndex++)
                    {
                        if (scheduleSubjectOfGroupNameAForRoom == null && scheduleSubjectOfGroupNameBForRoom == null) continue;

                        CurriculumSubjectWithCount subjectSortedGroupNameA;
                        if (scheduleSubjectOfGroupNameAForRoom == null)
                        {
                            subjectSortedGroupNameA = null;
                        }
                        else
                        {
                            subjectSortedGroupNameA = scheduleSubjectOfGroupNameAForRoom[dayOfWeek, classIndexA, slotIndex];
                        }

                        CurriculumSubjectWithCount subjectSortedGroupNameB;

                        if (scheduleSubjectOfGroupNameBForRoom == null)
                        {
                            subjectSortedGroupNameB = null;
                        }
                        else
                        {
                            subjectSortedGroupNameB = scheduleSubjectOfGroupNameBForRoom[dayOfWeek, classIndexB, slotIndex];
                        }


                        if (subjectSortedGroupNameA == null && subjectSortedGroupNameB == null) continue;

                        string typeSlot = ScheduleConstants.TypeSlotIsNew;

                        string slotTypeCodeA = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, dataContextInPartOfDay.PartOfDay, subjectSortedGroupNameA != null ? subjectSortedGroupNameA.Subject.TeachingMode : "");
                        string slotTypeCodeB = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, dataContextInPartOfDay.PartOfDay, subjectSortedGroupNameB != null ? subjectSortedGroupNameB.Subject.TeachingMode : "");

                        string statusSLotGroupNameA = "";
                        string statusSLotGroupNameB = "";

                        if (GetSlotTypeForWeek(week, dayOfWeek, slotTypeCodeA))
                        {
                            statusSLotGroupNameA = ScheduleConstants.StatusSlotIsOffline;
                            statusSLotGroupNameB = ScheduleConstants.StatusSlotIsOnline;
                        }
                        else
                        {
                            statusSLotGroupNameA = ScheduleConstants.StatusSlotIsOnline;
                            statusSLotGroupNameB = ScheduleConstants.StatusSlotIsOffline;
                        }

                        // Hợp nhất logic gọi hàm GetSessionNo
                        int sessionNoA = 0;
                        int sessionNoB = 0;

                        if (week == 1)
                        {
                            sessionNoA = subjectSortedGroupNameA != null ? subjectSortedGroupNameA.Count : -1;
                            sessionNoB = subjectSortedGroupNameB != null ? subjectSortedGroupNameB.Count : -1;
                        }
                        else
                        {
                            if (subjectSortedGroupNameA != null)
                            {
                                if (subjectSortedGroupNameA.Subject.TotalSlots == ScheduleConstants.TotalSlotsNomal)
                                {
                                    sessionNoA = subjectSortedGroupNameA.Count == 1 ? (2 * week - 1) : (2 * week); // Tuần 1 là slot thứ 1, tuần 2 là slot thứ 3, tuần 3 là slot thứ 5, v.v.
                                }
                                else
                                {
                                    sessionNoA = 0;
                                }
                            }

                            if (subjectSortedGroupNameB != null)
                            {
                                if (subjectSortedGroupNameB.Subject.TotalSlots == ScheduleConstants.TotalSlotsNomal)
                                {
                                    sessionNoB = subjectSortedGroupNameB.Count == 1 ? (2 * week - 1) : (2 * week); // Tuần 1 là slot thứ 1, tuần 2 là slot thứ 3, tuần 3 là slot thứ 5, v.v.
                                }
                                else
                                {
                                    sessionNoB = 0;
                                }
                            }
                        }

                        var (lecturerIdOfGroupNameA, lecturerNameOfGroupNameA, lecturerAccoutOfGroupNameA) = _getLecturerForSubject.FindLecturerForSubject(subjectSortedGroupNameA.Subject.SubjectCode, dataContextInPartOfDay.LecturersTeachSubjects, cycleLevelA);
                        var (lecturerIdOfGroupNameB, lecturerNameOfGroupNameB, lecturerAccoutOfGroupNameB) = _getLecturerForSubject.FindLecturerForSubject(subjectSortedGroupNameB.Subject.SubjectCode, dataContextInPartOfDay.LecturersTeachSubjects, cycleLevelB);

                        int slotLabel = slotIndex + slotStart;

                        //var schedulesItemOfGroupNameA = _treeNode.CollectSchedules(roomNode, subjectSortedGroupNameA.Subject.SubjectCode, currentDate, groupClassA.GroupName, slotLabel, lecturerIdOfGroupNameA, lecturerNameOfGroupNameA, slotTypeCodeA, typeSlot, sessionNoA, dataContextInPartOfDay.PartOfDay, statusSLotGroupNameA);
                        //var schedulesItemOfGroupNameB = _treeNode.CollectSchedules(roomNode, subjectSortedGroupNameB.Subject.SubjectCode, currentDate, groupClassB.GroupName, slotLabel, lecturerIdOfGroupNameB, lecturerNameOfGroupNameB, slotTypeCodeB, typeSlot, sessionNoB, dataContextInPartOfDay.PartOfDay, statusSLotGroupNameB);

                        //sessionSchedules.AddRange(schedulesItemOfGroupNameA);
                        //sessionSchedules.AddRange(schedulesItemOfGroupNameB);
                    }
                }
            }
            return sessionSchedules;
        }

        public async Task<List<Schedule>> GenerateSchedulesFullOffAsync(SchedulePartOfDayContext dataContextInPartOfDay)
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

        public int MapToCycleTwoClasses(int roomNo)
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
    }
}
