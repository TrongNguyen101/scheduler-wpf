using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.RoomService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Algorithm
{
    public class GenerateScheduleForAllDate
    {
        private readonly IRoomService _roomService;
        private readonly TreeForSchedule _treeNode;
        private readonly SortSubjectsOneSession _sortSubjectsOneSession;
        private readonly GetLecturerForSubject _getLecturerForSubject;
        private readonly CreateSlotTypeCode _createSlotTypeCode;

        public GenerateScheduleForAllDate(TreeForSchedule treeNode, GetLecturerForSubject getLecturerForSubject, SortSubjectsOneSession sortSubjectsOneSession, IRoomService roomService, CreateSlotTypeCode createSlotTypeCode)
        {
            _treeNode = treeNode;
            _sortSubjectsOneSession = sortSubjectsOneSession;
            _getLecturerForSubject = getLecturerForSubject;
            _roomService = roomService;
            _createSlotTypeCode = createSlotTypeCode;
        }
        public async Task<List<Schedule>> CreateSchedules(List<CurriculumSubject> subjects, List<GroupClass> listGroupName, List<LecturerSubject> lecturerSubject, DateTime startDate, List<LecturerRequest> lecturerRequests)
        {
            List<Schedule> allSchedules = new List<Schedule>();

            var (listGroupNameAm, listGroupNamePm) = BalancedSplitWithGreedySwap(listGroupName);

            // Cache lecturer lookup
            // có bao nhiêu ông thầy thì có bấy nhiêu lớp học cùng lúc
            var lecturersTeachSubject = lecturerSubject.GroupBy(l => l.SubjectCode).ToDictionary(g => g.Key, g => g.ToList());
            var lecturersAM = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "AM");
            var lecturersPM = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "PM");

            // Tạo lịch cho tuần đầu và tuần cuối - buổi sáng (AM)
            var firstAndLastWeekSchedulesAM = await CreateSchedulesForFirstAndFinalWeek(subjects, lecturersAM, listGroupNameAm, startDate, lecturerRequests, "A");
            allSchedules.AddRange(firstAndLastWeekSchedulesAM);

            // Tạo lịch cho tuần đầu và tuần cuối - buổi chiều (PM)
            var firstAndLastWeekSchedulesPM = await CreateSchedulesForFirstAndFinalWeek(subjects, lecturersPM, listGroupNamePm, startDate, lecturerRequests, "P");
            allSchedules.AddRange(firstAndLastWeekSchedulesPM);

            // Tạo lịch cho tuần từ 2 đến 9 - buổi sáng (AM)
            var weekSchedulesOnlineAm = await CreateSchedulesForWeek(subjects, lecturersAM, listGroupNameAm, startDate, lecturerRequests, "A");
            allSchedules.AddRange(weekSchedulesOnlineAm);

            var weekSchedulesOnlinePm = await CreateSchedulesForWeek(subjects, lecturersAM, listGroupNamePm, startDate, lecturerRequests, "P");
            allSchedules.AddRange(weekSchedulesOnlinePm);

            return allSchedules;
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
        /// <param name="curriculumSubjects">Danh sách các môn học.</param>
        /// <param name="lecturersTeachSubjectSession">Dictionary chứa danh sách giảng viên theo môn học cho session hiện tại.</param>
        /// <param name="numberOfRoom">Số lượng phòng cần tạo lịch.</param>
        /// <param name="startDate">Ngày bắt đầu của kỳ học.</param>
        /// <param name="lecturerRequests">Danh sách yêu cầu của giảng viên.</param>
        /// <param name="sessionFilter">Session hiện tại ("A" cho AM, "P" cho PM).</param>
        /// <param name="roomCodePrefix">Tiền tố mã phòng (ví dụ: "G").</param>
        /// <param name="classIdStartIndex">Chỉ số bắt đầu để sinh mã lớp.</param>
        private async Task<List<Schedule>> CreateSchedulesForFirstAndFinalWeek(List<CurriculumSubject> curriculumSubjects,
                                      Dictionary<string, List<LecturerSubject>> lecturersTeachSubjectSession,
                                      List<GroupClass> listGroupName,
                                      DateTime startDate,
                                      List<LecturerRequest> lecturerRequests,
                                      string sessionFilter)
        {
            List<Schedule> allSchedules = new List<Schedule>();


            // Dictionary dùng để tạo thứ tự từng slot học trong kỳ
            var subjectAppearanceOrder = new Dictionary<string, int>();

            // số thứ tự slot dựa vào buổi trong ngày
            int slotStart = sessionFilter == "A" ? 1 : 3;

            // số lượng slot trong 1 buổi
            int slotsPerPartOfDay = 2;

            int slotsPerWeek = 2;

            // Tạo kiểu onl hay off cho tuần đó
            string slotType = "";

            List<Room> listRooms = await _roomService.GetNumberOfRoom(listGroupName.Count);

            for (int indexRoom = 0; indexRoom < listRooms.Count; indexRoom++)
            {
                TreeForSchedule roomNode = await _treeNode.BuildTreeForRoom(listRooms[indexRoom].RoomId, listRooms[indexRoom].RoomName);

                /* Cần hàm tạo group name (mã lơp) ở đây*/
                string groupName = listGroupName[indexRoom].GroupName;

                var subjectOfClass = curriculumSubjects.Where(s => s.CurriculumCode == listGroupName[indexRoom].CurriculumCode && s.TermNo == listGroupName[indexRoom].Term).ToList();

                var scheduleSubjectForClass = _sortSubjectsOneSession.SortSubjectFourClassFlexibleSubject(subjectOfClass);

                // tìm thầy cho mỗi 4 lớp
                var (classIndex, cycleLevel) = MapToCycle(indexRoom + 1);

                // duyệt qua 10 tuần
                foreach (int week in new int[] { 1, 10 })
                {
                    //Duyệt qua 7 ngày trong tuần
                    for (int dayOfWeek = 1; dayOfWeek <= 7; dayOfWeek++)
                    {
                        //Lấy ngày tháng hiện tại của ngày
                        DateTime currentDate = startDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));

                        // duyệt qua 2 slot của 1 buổi
                        for (int slotIndex = 0; slotIndex < slotsPerPartOfDay; slotIndex++)
                        {
                            // Lấy môn học đã được xếp vào ngày slot hiện tại
                            var subject = scheduleSubjectForClass[dayOfWeek, classIndex, slotIndex];
                            if (subject == null) continue;

                            // Lấy mã loại slot dựa trên ngày, slot và buổi
                            //string slotTypeCode = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, sessionFilter, subject.TeachingMode);

                            //if (GetSlotTypeForWeek(week, dayOfWeek, slotTypeCode))
                            //{
                            //    slotType = "offline";
                            //}

                            //// Lấy thứ tự buổi học trong kỳ
                            //int sessionNo = GetSessionNoForFirstWeeksAndFinal(subjectAppearanceOrder, subject, week);

                            //// lấy tên giảng viên để thêm vào lịch
                            //var (lecturerId, lecturerName, lecturerAccount) = _getLecturerForSubject.FindLecturerForSubject(
                            //    subject?.SubjectCode, lecturersTeachSubjectSession, cycleLevel);

                            //int slotLabel = slotIndex + slotStart;

                            //var schedulesItem = _treeNode.CollectSchedules(roomNode, subject.SubjectCode, currentDate, groupName, slotLabel, lecturerId, lecturerName, slotTypeCode, "NewSlot", sessionNo, sessionFilter, slotType);

                            //allSchedules.AddRange(schedulesItem);
                        }
                    }
                }
            }
            return allSchedules;
        }
        
        private async Task<List<Schedule>> CreateSchedulesForWeek(List<CurriculumSubject> curriculumSubjects,
                                      Dictionary<string, List<LecturerSubject>> lecturersTeachSubjectSession,
                                      List<GroupClass> listGroupName,
                                      DateTime startDate,
                                      List<LecturerRequest> lecturerRequests,
                                      string sessionFilter)
        {
            List<Schedule> allSchedules = new List<Schedule>();

            // Dictionary dùng để tạo thứ tự từng slot học trong kỳ
            var subjectAppearanceOrder = new Dictionary<string, int>();

            // số thứ tự slot dựa vào buổi trong ngày
            int slotStart = sessionFilter == "A" ? 1 : 3;

            // số lượng slot trong 1 buổi
            int slotsPerPartOfDay = 2;

            int slotsPerWeek = 2;



            // Chia danh sách lớp thành 2 nhóm
            var groupBBA = listGroupName.Where(x => x.Department == "BBA").ToList();
            var groupBIT_NN = listGroupName.Where(x => x.Department == "BIT" || x.Department == "NN").ToList();

            // Tính số phòng theo nhóm có nhiều lớp hơn
            int numberOfRoomsForAllClass = Math.Max(groupBBA.Count, groupBIT_NN.Count);

            // Tạo kiểu onl hay off cho tuần đó
            string slotTypeBBA = "";
            string slotTypeBIT_NN = "";

            List<Room> listRooms = await _roomService.GetNumberOfRoom(numberOfRoomsForAllClass);


            for (int indexRoom = 0; indexRoom < listRooms.Count; indexRoom++)
            {
                TreeForSchedule roomNode = await _treeNode.BuildTreeForRoom(listRooms[indexRoom].RoomId, listRooms[indexRoom].RoomName);

                string groupNameInGroupBBA = groupBBA[indexRoom].GroupName;
                string groupNameInGroupBIT_NN = groupBIT_NN[indexRoom].GroupName;

                var subjectOfClassBBA = curriculumSubjects.Where(s => s.CurriculumCode == groupBBA[indexRoom].CurriculumCode && s.TermNo == groupBBA[indexRoom].Term).ToList();
                var subjectOfClassBIT_NN = curriculumSubjects.Where(s => s.CurriculumCode == groupBIT_NN[indexRoom].CurriculumCode && s.TermNo == groupBIT_NN[indexRoom].Term).ToList();

                var scheduleSubjectForClassBBA = _sortSubjectsOneSession.SortSubjectFourClass(subjectOfClassBBA);
                var scheduleSubjectForClassBIT_NN = _sortSubjectsOneSession.SortSubjectFourClass(subjectOfClassBIT_NN);

                // tìm thầy cho mỗi 4 lớp
                var (classIndex, cycleLevel) = MapToCycle(indexRoom + 1);

                // duyệt qua 10 tuần
                for (int week = 2; week <= 9; week++)
                {
                    
                    //Duyệt qua 7 ngày trong tuần
                    for (int dayOfWeek = 1; dayOfWeek <= 7; dayOfWeek++)
                    {
                        //Lấy ngày tháng hiện tại của ngày
                        DateTime currentDate = startDate.AddDays((week - 1) * 7 + (dayOfWeek - 1));
                        

                        // duyệt qua 2 slot của 1 buổi
                        for (int slotIndex = 0; slotIndex < slotsPerPartOfDay; slotIndex++)
                        {
                            // Lấy môn học đã được xếp vào ngày slot hiện tại
                            var subjectBBA = scheduleSubjectForClassBBA[dayOfWeek, classIndex, slotIndex];
                            var subjectBIT_NN = scheduleSubjectForClassBIT_NN[dayOfWeek, classIndex, slotIndex];

                            if (subjectBBA == null && subjectBIT_NN == null) continue;

                            // Lấy mã loại slot dựa trên ngày, slot và buổi
                            //string slotTypeCode = _createSlotTypeCode.GetSlotTypeCode(dayOfWeek + 1, slotIndex + 1, sessionFilter, subjectBBA.TeachingMode);

                            //if (GetSlotTypeForWeek(week, dayOfWeek, slotTypeCode))
                            //{
                            //    slotTypeBBA = "offline";
                            //    slotTypeBIT_NN = "online";
                            //}
                            //else
                            //{
                            //    slotTypeBBA = "online";
                            //    slotTypeBIT_NN = "offline";
                            //}


                        //    // Lấy thứ tự buổi học trong kỳ
                        //    int sessionNoBBA = GetSessionNoForWeeks(subjectAppearanceOrder, subjectBBA);
                        //    int sessionNoBIT_NN = GetSessionNoForWeeks(subjectAppearanceOrder, subjectBIT_NN);

                        //    // lấy tên giảng viên để thêm vào lịch
                        //    var (lecturerBBAId, lecturerBBAName, lecturerBBAAccount) = _getLecturerForSubject.FindLecturerForSubject(
                        //        subjectBBA?.SubjectCode, lecturersTeachSubjectSession, cycleLevel);
                        //    var (lecturerBITId, lecturerBITName, lecturerBITAccount) = _getLecturerForSubject.FindLecturerForSubject(
                        //        subjectBIT_NN?.SubjectCode, lecturersTeachSubjectSession, cycleLevel);

                        //    int slotLabel = slotIndex + slotStart;


                        //    var schedulesItemBBA = _treeNode.CollectSchedules(roomNode, subjectBBA.SubjectCode, currentDate, groupNameInGroupBBA, slotLabel, lecturerBBAId, lecturerBBAName, slotTypeCode, "NewSlot", sessionNoBBA, sessionFilter, slotTypeBBA);
                        //    var schedulesItemBIT_NN = _treeNode.CollectSchedules(roomNode, subjectBIT_NN.SubjectCode, currentDate, groupNameInGroupBIT_NN, slotLabel, lecturerBITId, lecturerBITName, slotTypeCode, "NewSlot", sessionNoBIT_NN, sessionFilter, slotTypeBIT_NN);

                        //    allSchedules.AddRange(schedulesItemBBA);
                        //    allSchedules.AddRange(schedulesItemBIT_NN);
                        }
                    }
                }
            }
            return allSchedules;
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
                    } else
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


        private int CalculateNumberOfRoomsForFirstAndFinalWeek(int numberOfClass)
        {
            return (numberOfClass + 1) / 2; // Làm tròn lên, tối ưu hơn
        }

        private int CalculateNumberOfRoomsForWeeks(int numberOfClass)
        {
            return ((numberOfClass + 1) / 2) / 2; // Làm tròn lên, tối ưu hơn
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
