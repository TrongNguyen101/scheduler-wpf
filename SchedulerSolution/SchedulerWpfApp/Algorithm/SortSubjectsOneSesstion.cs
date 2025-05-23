using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class SortSubjectsOneSession
    {
        public Subject[,] SortSubjectsOneSesstion(List<Subject> allSubjects)
        {

            //  get all subjects with 2 slots per week
            var subjects = allSubjects.Where(subject => subject.SlotsPerWeek == 2).ToList();
            var subjectOneSlot = allSubjects.Where(subject => subject.SlotsPerWeek == 1).ToList();

            Subject[,] schedule = new Subject[8, 2];
            schedule[1, 0] = subjects[0]; // monday slot 1
            schedule[3, 1] = subjects[0]; // wednesday slot 2
            schedule[1, 1] = subjects[1]; // monday slot 2
            schedule[3, 0] = subjects[1]; // wednesday slot 1
            schedule[2, 0] = subjects[2]; // tuesday slot 1
            schedule[4, 1] = subjects[2]; // thursday slot 2
            schedule[2, 1] = subjects[3]; // tuesday slot 2
            schedule[4, 0] = subjects[3]; // thursday slot 1
            schedule[5, 0] = subjectOneSlot[0]; // friday slot 1

            return schedule;
        }

        public Subject[,,] SortSubjectFourClass(List<Subject> allSubjects, int numClasses = 4)
        {

            // trong một tuần, trong 1 buổi, một thầy dạy được tối đa 4 lớp mỗi lớp 2 slot cho 1 môn

            //  get all subjects with 2 slots per week
            var subjects = allSubjects.Where(subject => subject.SlotsPerWeek == 2).ToList();
            var subjectOneSlot = allSubjects.Where(subject => subject.SlotsPerWeek == 1).ToList();

            // Kiểm tra đầu vào
            if (subjects == null || subjects.Count != 4)
            {
                throw new ArgumentException("Course array must contain exactly 4 courses.");
            }
            if (numClasses < 1 || numClasses > 4)
            {
                throw new ArgumentException("Number of classes must be between 1 and 4.");
            }

            // Khởi tạo lịch học: [Ngày, Lớp, Slot]
            Subject[,,] schedule = new Subject[8, numClasses, 2];

            // Mẫu xoay vòng cho 4 lớp (chỉ số môn học: 0, 1, 2, 3)
            int[,] mondayPattern = new int[4, 2] {
            { 0, 1 }, // Class 01: Slot 1: SWT301, Slot 2: SWP391
            { 1, 0 }, // Class 02: Slot 1: SWP391, Slot 2: SWT301
            { 2, 3 }, // Class 03: Slot 1: SWR321, Slot 2: PRN211
            { 3, 2 }  // Class 04:Slot 1: PRN211, Slot 2: SWR321
            };

            int[,] tuesdayPattern = new int[4, 2] {
            { 2, 3 }, // Class 01: Slot 1: SWR321, Slot 2: PRN211
            { 3, 2 }, // Class 02: Slot 1: PRN211, Slot 2: SWR321
            { 0, 1 }, // Class 03: Slot 1: SWT301, Slot 2: SWP391
            { 1, 0 }  // Class 04:Slot 1: SWP391, Slot 2: SWT301
            };

            // Gán lịch cho thứ 2
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[1, classIndex, 0] = subjects[mondayPattern[classIndex, 0]]; // Slot 1
                schedule[1, classIndex, 1] = subjects[mondayPattern[classIndex, 1]]; // Slot 2
            }

            // Gán lịch cho thứ 3
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[2, classIndex, 0] = subjects[tuesdayPattern[classIndex, 0]]; // Slot 1
                schedule[2, classIndex, 1] = subjects[tuesdayPattern[classIndex, 1]]; // Slot 2
            }

            // Thứ 4: Lặp lại thứ 2 nhưng đổi slot
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[3, classIndex, 0] = schedule[1, classIndex, 1]; // Slot 1 thứ 4 = Slot 2 thứ 2
                schedule[3, classIndex, 1] = schedule[1, classIndex, 0]; // Slot 2 thứ 4 = Slot 1 thứ 2
            }

            // Thứ 5: Lặp lại thứ 3 nhưng đổi slot
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[4, classIndex, 0] = schedule[2, classIndex, 1]; // Slot 1 thứ 5 = Slot 2 thứ 3
                schedule[4, classIndex, 1] = schedule[2, classIndex, 0]; // Slot 2 thứ 5 = Slot 1 thứ 3
            }

            // Gán môn thứ 5 vào thứ 6 hoặc thứ 7, slot 1 hoặc slot 2
            (int day, int slot)[] fifthCourseSlots = new (int, int)[]
            {
            (5, 0), // Thứ 6, Slot 1
            (5, 1), // Thứ 6, Slot 2
            (6, 0), // Thứ 7, Slot 1
            (6, 1)  // Thứ 7, Slot 2
            };

            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                var (day, slot) = fifthCourseSlots[classIndex];
                schedule[day, classIndex, slot] = subjectOneSlot[0]; // Gán môn thứ 5
            }

            return schedule;
        }
    }
}
