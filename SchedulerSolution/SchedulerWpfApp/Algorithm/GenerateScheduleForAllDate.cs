using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;

namespace SchedulerWpfApp.Algorithm
{
    public class GenerateScheduleForAllDate
    {
        private readonly TreeForSchedule _treeNode;
        private readonly SortSubjectsOneSession _sortSubjectsOneSession;
        private readonly GetLecturerForSubject _getLecturerForSubject;

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

            // 1. Tổng hợp danh sách vị trí xuất hiện của từng môn học
            var subjectAppearances = new Dictionary<string, List<(int Day, int Slot)>>();
            var subjectAppearanceOrder = new Dictionary<string, int>(); // Đếm lần xuất hiện

            for (int day = 1; day <= 7; day++)
            {
                for (int classIndex = 0; classIndex < 4; classIndex++) // giả định 4 lớp
                {
                    for (int slot = 0; slot < 2; slot++)
                    {
                        var subject = subjectSchedule[day, classIndex, slot];
                        if (subject == null || string.IsNullOrEmpty(subject.SubjectCode)) continue;

                        if (!subjectAppearances.ContainsKey(subject.SubjectCode))
                            subjectAppearances[subject.SubjectCode] = new();

                        subjectAppearances[subject.SubjectCode].Add((day + 1, slot + 1));
                    }
                }
            }

            for (int roomNo = 1; roomNo <= numberOfRoom; roomNo++)
            {
                var roomId = $"{roomCodePrefix}{roomNo}";
                TreeForSchedule roomNode = _treeNode.BuildTreeForRoom(roomId);
                string classId = $"SE160{classIdStartIndex + roomNo - 1}";
                var (classIndex, cycleLevel) = MapToCycle(roomNo);
                int sessionNo = 0;

                for (int week = 1; week <= 10; week++)
                {
                    string slotType = GetSlotTypeForWeek(week);

                    for (int day = 1; day <= 7; day++)
                    {
                        DateTime currentDate = startDate.AddDays((week - 1) * 7 + (day - 1));
                        var lecturersForDay = _getLecturerForSubject.FilterLecturerInDate(
                            lecturersTeachSubjectSession, lecturerRequests, sessionFilter, currentDate);

                        int slotStart = sessionFilter == "A" ? 1 : 3;
                        int slotsCount = 2;

                        for (int slot = 0; slot < slotsCount; slot++)
                        {
                            var subject = subjectSchedule[day, classIndex, slot];

                            string slotTypeCode = "";
                            if (subject != null && subjectAppearances.TryGetValue(subject.SubjectCode, out var appearances))
                            {
                                var parts = appearances.Select(a => $"{a.Day}{a.Slot}");
                                slotTypeCode = $"{sessionFilter}{string.Join("", parts)}";

                                // Tăng thứ tự xuất hiện
                                if (subject != null && !subjectAppearanceOrder.ContainsKey(subject.SubjectCode))
                                {
                                    subjectAppearanceOrder[subject.SubjectCode] = 1;
                                }
                                else
                                {
                                    if (subjectAppearanceOrder[subject.SubjectCode] >= subject.TotalSessions)
                                    {
                                        subjectAppearanceOrder[subject.SubjectCode] = 1;
                                    }
                                    else
                                    {
                                        subjectAppearanceOrder[subject.SubjectCode]++;
                                    }
                                }

                                sessionNo = subjectAppearanceOrder[subject.SubjectCode];
                            }

                            var lecturerName = _getLecturerForSubject.FindLecturerForSubject(
                                subject?.SubjectCode, lecturersForDay, cycleLevel);


                            _treeNode.CollectSchedules(
                                roomNode,
                                allSchedules,
                                subject?.SubjectName ?? "No subject",
                                currentDate,
                                classId,
                                $"slot {slot + slotStart}",
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


        private int CalculateNumberOfRooms(int numberOfClass)
        {
            return (numberOfClass + 1) / 2; // Làm tròn lên, tối ưu hơn
        }

        private string GetSlotTypeForWeek(int week)
        {
            return week % 2 == 0 ? "online" : "offline";
        }


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
