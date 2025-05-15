using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Services
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
                int numberOfRoom = 0;

                string slotType = string.Empty;

                // If the number of classes is odd, add one extra room to accommodate the remaining class
                if (numberOfClass % 2 != 0)
                {
                    numberOfRoom = numberOfClass / 2 + 1;
                }
                else
                {
                    // If the number of classes is even, divide the classes evenly between the rooms
                    numberOfRoom = numberOfClass / 2;
                }

                // Generate a schedule for subjects for one session
                var subjectSchedule = _sortSubjectsOneSession.SortSubjectFourClass(subjects);

                // Cache lecturer lookup
                // có bao nhiêu ông thầy thì có bấy nhiêu lớp học cùng lúc
                var lecturersTeachSubject = lecturerSubject.GroupBy(l => l.SubjectCode).ToDictionary(g => g.Key, g => g.ToList());
                var lecturersTeachSubjectInAMSession = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "AM");
                var lecturersTeachSubjectInPMSession = _getLecturerForSubject.FilterLecturerInSession(lecturersTeachSubject, lecturerRequests, "PM");

                // Loop through the number of rooms needed
                for (int roomNo = 1; roomNo <= numberOfRoom; roomNo++)
                {
                    string currentSessionFilter = "AM"; // Filter for AM session
                    // Build a binary tree for the current room
                    TreeForSchedule roomNode = _treeNode.BuildTreeForRoom($"G20{roomNo}");

                    // Generate a class ID for the current room
                    var classId = $"SE160{roomNo}";
                    var currentDate = startDate;

                    // Calculate the class index and cycle level based on the room number (one class per room)
                    // For example:
                    // 5: position = 1, cycle = 1, level = 1
                    // 6: position = 2, cycle = 1, level = 1
                    // 10: position = 2, cycle = 2, level = 2
                    // 14: position = 2, cycle = 3, level = 3
                    var (classIndex, cycleLevel) = MapToCycle(roomNo);

                    for (int week = 1; week <= 10; week++)
                    {
                        if (week % 2 == 0)
                        {
                            slotType = "online"; // Filter for PM session
                        }
                        else
                        {
                            slotType = "offline"; // Filter for AM session
                        }
                        // Iterate through the days of the week (Monday to Sunday)
                        for (int day = 1; day <= 7; day++)
                        {
                            var lecturerTeachSubjectInDayAM = _getLecturerForSubject.FilterLecturerInDate(lecturersTeachSubjectInAMSession, lecturerRequests, currentSessionFilter, currentDate);
                            // Iterate through the two slots (AM session)
                            for (int slot = 0; slot < 2; slot++)
                            {
                                var subject = subjectSchedule[day, classIndex, slot];

                                var lecturerName = _getLecturerForSubject.FindLecturerForSubject(subject?.SubjectCode, lecturerTeachSubjectInDayAM, cycleLevel);

                                _treeNode.CollectSchedules(roomNode, allSchedules, subject?.SubjectName ?? "No subject",
                                                 currentDate, classId, $"slot {slot + 1}",
                                                 lecturerName,"A24", "NewSlot", "1", currentSessionFilter, slotType);
                            }
                            // Move to the next day
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                }

                // Loop through the number of rooms needed
                for (int roomNo = 1; roomNo <= numberOfRoom; roomNo++)
                {
                    string currentSessionFilter = "PM"; // Filter for PM session
                    // Build a binary tree for the current room
                    TreeForSchedule roomNode = _treeNode.BuildTreeForRoom($"G20{roomNo}");

                    // Generate a class ID for the current room, offset by the number of rooms
                    var classId = $"SE160{roomNo + numberOfRoom}";

                    // Start scheduling from the given date
                    var currentDate = startDate;

                    // Calculate the class index and cycle level based on the room number (one class per room)
                    // For example:
                    // 5: position = 1, cycle = 1, level = 1
                    // 6: position = 2, cycle = 1, level = 1
                    // 10: position = 2, cycle = 2, level = 2
                    // 14: position = 2, cycle = 3, level = 3
                    var (classIndex, cycleLevel) = MapToCycle(roomNo);

                    for (int week = 1; week <= 10; week++)
                    {
                        if (week % 2 == 0)
                        {
                            slotType = "online"; // Filter for PM session
                        }
                        else
                        {
                            slotType = "offline"; // Filter for AM session
                        }
                        // Iterate through the days of the week (Monday to Sunday)
                        for (int day = 1; day <= 7; day++)
                        {
                            var lecturerTeachSubjectInDayPM = _getLecturerForSubject.FilterLecturerInDate(lecturersTeachSubjectInPMSession, lecturerRequests, currentSessionFilter, currentDate);
                            // Iterate through the two slots (PM session)
                            for (int slot = 0; slot < 2; slot++)
                            {
                                var subject = subjectSchedule[day, classIndex, slot];
                                var lecturerName = _getLecturerForSubject.FindLecturerForSubject(subject?.SubjectCode, lecturerTeachSubjectInDayPM, cycleLevel);
                                _treeNode.CollectSchedules(roomNode, allSchedules, subject?.SubjectName ?? "No subject",
                                                 currentDate, classId, $"slot {slot + 3}",
                                                 lecturerName, "P64", "NewSlot", "1", currentSessionFilter, slotType);
                            }
                            // Move to the next day
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating schedules: {ex}");
            }
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
                int cyclePosition = ((roomNo - 1) % 4) + 1;
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
