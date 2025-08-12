using Microsoft.Extensions.Logging;
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

        private readonly ScheduleCommonSubjectVersion3 _scheduleCommonSubjectVersion3;
        private readonly RoomSchedulerOnOff _roomSchedulerOnOff;

        private SchedulingContext _context; // Lưu trữ ngữ cảnh đã chuẩn bị

        public Version2CreateSchedule(ILogger<Version2CreateSchedule> logger,
                                      IScheduleServices scheduleServices,
                                      ILecturerSubjectServices lecturerSubjectServices,
                                      ILecturerServices lecturerServices,
                                      IGroupNameService groupNameService,
                                      ICurriculumSubjectServices curriculumSubjectServices,
                                      IRoomService roomService,
                                      SchedulingContext context,
                                      ScheduleCommonSubjectVersion3 scheduleCommonSubjectVersion3,
                                      RoomSchedulerOnOff roomSchedulerOnOff)
        {
            _logger = logger;
            _scheduleServices = scheduleServices;
            _lecturerSubjectServices = lecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameService;
            _curriculumSubjectServices = curriculumSubjectServices;
            _context = context;
            _scheduleCommonSubjectVersion3 = scheduleCommonSubjectVersion3;
            _roomService = roomService;
            _roomSchedulerOnOff = roomSchedulerOnOff;
            _roomSchedulerOnOff = roomSchedulerOnOff;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate, List<string> listMajorGroupA, List<string> listMajorGroupB, Progress<int> progress)
        {
            try
            {
                _context = await PrepareSchedulingDataAsync();

                if (_context == null || !_context.Rooms.Any() || !_context.GroupNames.Any() || !_context.CurriculumSubjects.Any())
                {
                    _logger.LogWarning("Context or required data is missing.");
                    return new List<Schedule>(); // Trả về danh sách rỗng nếu không có phòng
                }

                List<GroupClass> listGroupNameNoOJT = _context.GroupNames.Where(g => g.TeachingMode != "OJT").ToList();
                var allSchedules = new List<Schedule>();
                List<int> firstAndFinalWeek = new List<int> { 9 };
                (List<Schedule> templateSchedules, List<string> Conflicts, int TotalPlannedSchedules,
                int TotalUnits,
                int ScheduledUnits,
                int CompletionPercent,
                List<(string GroupName, string SubjectCode)> UnscheduledUnits) = _scheduleCommonSubjectVersion3.GenerateSchedule(listGroupNameNoOJT, _context.LecturerSubjects, _context.CurriculumLookup, startDate, progress);

                var schedulesOfFirstAndFinalWeek = templateSchedules;

                _logger.LogWarning($"Total Conflicts: {Conflicts.Count}");
                foreach (var conflict in Conflicts)
                {
                    _logger.LogWarning(conflict);
                }
                _logger.LogWarning($"Total Units: {TotalUnits}");
                _logger.LogWarning($"Total UnscheduledUnits: {UnscheduledUnits.Count}");
                _logger.LogWarning($"Completion Percent: {CompletionPercent}%");


                AssignRooms(schedulesOfFirstAndFinalWeek, _context.Rooms);

                allSchedules.AddRange(GenerateFullSchedule(schedulesOfFirstAndFinalWeek));

                allSchedules.AddRange(_roomSchedulerOnOff.GenerateSchedulesForSubsequentWeeks(templateSchedules, _context.Rooms, _context.GroupNames));

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
                LecturerSubjects = lecturersSubjects,
                Rooms = listRoom,
                CurriculumSubjects = curriculumSubjects,
                GroupNames = listGroupName,
                CurriculumLookup = curriculumLookup,
                LecturersTeachSubjects = lecturersTeachSubjects,
            };
        }

        // Hàm trợ giúp để tìm và lấy phòng ưu tiên
        private Room? FindAndRemovePreferredRoom(List<Room> availableRooms)
        {
            // 1. Ưu tiên tìm phòng ở tòa "G" trước
            Room? room = availableRooms.FirstOrDefault(r => r.Building == "G");

            // 2. Nếu không có phòng tòa "G", lấy bất kỳ phòng nào còn lại
            room ??= availableRooms.FirstOrDefault();

            // 3. Nếu tìm thấy một phòng, hãy xóa nó khỏi danh sách có sẵn để không dùng lại
            if (room != null)
            {
                availableRooms.Remove(room);
            }

            return room;
        }

        public void AssignRooms(List<Schedule> schedules, List<Room> allRooms)
        {
            // 1. Nhóm các lịch học theo GroupName
            var groups = schedules.ToLookup(s => s.GroupName);
            var allGroupNames = groups.Select(g => g.Key).ToList();

            // 2. Phân loại nhóm ưu tiên và nhóm thường
            var priorityGroupNames = allGroupNames
                .Where(name => groups[name].Any(s => s.Major.Contains("BIT") || s.Major.Contains("BBA")))
                .ToList();

            var generalGroupNames = allGroupNames.Except(priorityGroupNames).ToList();

            // 3. Chuẩn bị nguồn phòng cho mỗi buổi
            var availableRoomsA = new List<Room>(allRooms);
            var availableRoomsP = new List<Room>(allRooms);

            // Dictionary để lưu phòng đã gán cho mỗi nhóm trong mỗi buổi
            var assignments = new Dictionary<(string GroupName, string PartOfDay), Room>();

            // 4. Hàm nội bộ để thực hiện việc gán phòng cho một tập các nhóm
            Action<List<string>> assignRoomsForSet = (groupNames) =>
            {
                foreach (var groupName in groupNames)
                {
                    var groupSchedules = groups[groupName];

                    // Kiểm tra xem nhóm có lịch buổi sáng không
                    if (groupSchedules.Any(s => s.PartOfDay == "A"))
                    {
                        var room = FindAndRemovePreferredRoom(availableRoomsA);
                        if (room != null)
                        {
                            assignments[(groupName, "A")] = room;
                        }
                    }

                    // Kiểm tra xem nhóm có lịch buổi chiều không
                    if (groupSchedules.Any(s => s.PartOfDay == "P"))
                    {
                        var room = FindAndRemovePreferredRoom(availableRoomsP);
                        if (room != null)
                        {
                            assignments[(groupName, "P")] = room;
                        }
                    }
                }
            };

            // 5. Chạy logic gán phòng: ưu tiên trước, thường sau
            assignRoomsForSet(priorityGroupNames);
            assignRoomsForSet(generalGroupNames);

            // 6. Cập nhật lại phòng cho từng lịch học trong danh sách gốc
            foreach (var schedule in schedules)
            {
                if (assignments.TryGetValue((schedule.GroupName, schedule.PartOfDay), out var assignedRoom))
                {
                    schedule.RoomId = assignedRoom.RoomId;
                    schedule.RoomName = assignedRoom.RoomName;
                }
            }
        }

        /// <summary>
        /// Nhân bản lịch từ tuần đầu tiên cho các tuần tiếp theo.
        /// </summary>
        /// <param name="firstWeekSchedules">Danh sách lịch của tuần đầu tiên.</param>
        /// <param name="totalWeeks">Tổng số tuần cần tạo (ví dụ: 10).</param>
        /// <returns>Danh sách lịch hoàn chỉnh cho tất cả các tuần.</returns>
        private List<Schedule> GenerateFullSchedule(List<Schedule> firstWeekSchedules)
        {
            var fullSchedule = new List<Schedule>(firstWeekSchedules);
            int finalWeek = 10; // Tuần cuối cùng cần tạo lịch, vì tuần 0 là tuần gốc

            // weekIndex bắt đầu từ 1 vì tuần 0 là tuần gốc
            // Với mỗi lịch trong tuần đầu tiên...
            foreach (var originalSchedule in firstWeekSchedules)
            {
                // ...tạo một bản sao mới
                var newSchedule = CreateNewSchedule(originalSchedule, finalWeek);

                if (newSchedule.SessionNo == 2)
                {
                    newSchedule.SessionNo = 2 * finalWeek;
                }
                else
                {
                    newSchedule.SessionNo = (2 * finalWeek) - 1;
                }

                // Thêm lịch của tuần mới vào danh sách tổng
                fullSchedule.Add(newSchedule);
            }

            return fullSchedule;
        }

        private Schedule CreateNewSchedule(Schedule originalSchedule, int weekIndex)
        {
            var newSchedule = new Schedule
            {
                ScheduleId = originalSchedule.ScheduleId,
                RoomId = originalSchedule.RoomId,
                RoomName = originalSchedule.RoomName,
                PartOfDay = originalSchedule.PartOfDay,
                SlotTime = originalSchedule.SlotTime,
                StatusSlot = originalSchedule.StatusSlot,
                Date = originalSchedule.Date.GetValueOrDefault().AddDays((weekIndex - 1) * 7),
                Major = originalSchedule.Major,
                SubjectCode = originalSchedule.SubjectCode,
                GroupName = originalSchedule.GroupName,
                LecturerId = originalSchedule.LecturerId,
                LecturerName = originalSchedule.LecturerName,
                LecturerAccount = originalSchedule.LecturerAccount,
                TypeSlot = originalSchedule.TypeSlot,
                SessionNo = originalSchedule.SessionNo,
                SlotTypeCode = originalSchedule.SlotTypeCode,
                TermInYear = originalSchedule.TermInYear,
            };
            return newSchedule;
        }
    }
}
