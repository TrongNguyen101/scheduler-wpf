using SchedulerWpfApp.Algorithm.DTO;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Algorithm
{
    public class SortSubjectsOneSession
    {
        public CurriculumSubjectWithCount[,,] SortSubjectFourClass(List<CurriculumSubject> allSubjects, int numClasses = 4)
        {

            // trong một tuần, trong 1 buổi, một thầy dạy được tối đa 4 lớp mỗi lớp 2 slot cho 1 môn

            //  get all subjects with 2 slots per week

            //Hãy sửa lại chỗ này để chọn đúng môn
            var subjects = allSubjects.Where(subject => subject.TeachingMode == "ON/OFF").ToList();
            var subjectOneSlot = allSubjects.Where(subject => subject.TeachingMode == "C-ON").ToList();


            //Kiểm tra đầu vào
            if (subjects == null || subjects.Count != 4)
            {
                throw new ArgumentException("Course array must contain exactly 4 courses.");
            }
            if (numClasses < 1 || numClasses > 4)
            {
                throw new ArgumentException("Number of classes must be between 1 and 4.");
            }

            // Khởi tạo lịch học: [Ngày, Lớp, Slot]
            CurriculumSubjectWithCount[,,] schedule = new CurriculumSubjectWithCount[8, numClasses, 2];

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
                schedule[1, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[mondayPattern[classIndex, 0]],
                    Count = 1 // slot thứ 1 trong tuần
                }; // Slot 1
                schedule[1, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[mondayPattern[classIndex, 1]], // Slot 2
                    Count = 1 // slot thứ 1 trong tuần
                }; // Slot 2
            }

            // Gán lịch cho thứ 3
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[2, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[tuesdayPattern[classIndex, 0]],
                    Count = 1 // slot thứ 1 trong tuần
                }; // Slot 1

                schedule[2, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[tuesdayPattern[classIndex, 1]], // Slot 2
                    Count = 1 // slot thứ 1 trong tuần
                }; // Slot 2 
            }

            // Thứ 4: Lặp lại thứ 2 nhưng đổi slot
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[3, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[mondayPattern[classIndex, 1]], // Slot 1
                    Count = 2 // slot thứ 2 trong tuần
                }; // Slot 1 thứ 4 = Slot 2 thứ 2


                schedule[3, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[mondayPattern[classIndex, 0]],  // Slot 2
                    Count = 2 // slot thứ 2 trong tuần
                }; // Slot  2 thứ 4 = Slot 1 thứ 2
            }

            // Thứ 5: Lặp lại thứ 3 nhưng đổi slot
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[4, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[tuesdayPattern[classIndex, 1]], // Slot 1
                    Count = 2 // slot thứ 2 trong tuần
                }; // Slot 1 thứ 5 = Slot 2 thứ 3

                schedule[4, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = subjects[tuesdayPattern[classIndex, 0]], // Slot 2
                    Count = 2 // slot thứ 1 trong tuần
                }; // Slot 2 thứ 5 = Slot 1 thứ 3

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
                schedule[day, classIndex, slot] = new CurriculumSubjectWithCount
                {
                    Subject = subjectOneSlot[0],
                    Count = 1 // slot thứ 1 trong tuần
                };
            }

            return schedule;
        }

        public CurriculumSubjectWithCount[,,] SortFiveSubjectForClass(List<CurriculumSubject> allSubjects, int numClasses = 2)
        {

            // Khởi tạo lịch học: [Ngày, Lớp, Slot]
            CurriculumSubjectWithCount[,,] schedules = new CurriculumSubjectWithCount[8, numClasses, 2];

            // Mẫu xoay vòng cho 4 lớp (chỉ số môn học: 0, 1, 2, 3)
            int[,] mondayPattern = new int[2, 2] {
            { 0, 1 }, // Class 01: Slot 1: A1, Slot 2: A5
            { 1, 0 }, // Class 02: Slot 1: A5, Slot 2: A1
            };

            int[,] tuesdayPattern = new int[2, 2] {
            { 2, 3 }, // Class 01: Slot 1: A3, Slot 2: A4
            { 3, 2 }, // Class 02: Slot 1: A4, Slot 2: A3
            };

            int[,] wednesdayPattern = new int[2, 2] {
            { 4, 0 }, // Class 01: Slot 1: A6, Slot 2: A1
            { 0, 4 }, // Class 02: Slot 1: A1, Slot 2: A6
            };

            int[,] fridayPattern = new int[2, 2] {
            { 1, 4 }, // Class 01: Slot 1: A5, Slot 2: A6
            { 4, 1 }, // Class 02: Slot 1: A6, Slot 2: A5
            };

            // Gán lịch cho thứ 2
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedules[1, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[mondayPattern[classIndex, 0]], // Slot 1
                    Count = 1 // slot thứ 1 trong tuần
                };
                schedules[1, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[mondayPattern[classIndex, 1]], // Slot 2
                    Count = 1 // slot thứ 1 trong tuần
                };
            }

            // Gán lịch cho thứ 3
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedules[2, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[tuesdayPattern[classIndex, 0]], // Slot 1
                    Count = 1 // slot thứ 1 trong tuần
                };
                schedules[2, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[tuesdayPattern[classIndex, 1]], // Slot 2
                    Count = 1 // slot thứ 1 trong tuần
                };
            }

            // Gán lịch cho thứ 4
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedules[3, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[wednesdayPattern[classIndex, 0]], // Slot 1
                    Count = 1 // slot thứ 1 trong tuần
                };
                schedules[3, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[wednesdayPattern[classIndex, 1]], // Slot 2
                    Count = 2 // Slot 2
                };
            }

            // Thứ 5: Lặp lại thứ 3 nhưng đổi slot
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedules[4, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[tuesdayPattern[classIndex, 1]], // Slot 1
                    Count = 2 // slot thứ 2 trong tuần
                };
                schedules[4, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[tuesdayPattern[classIndex, 0]], // Slot 2
                    Count = 2 // slot thứ 2 trong tuần
                };
            }

            // Gán lịch cho thứ 4
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedules[5, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[fridayPattern[classIndex, 0]], // Slot 1
                    Count = 2 // slot thứ 2 trong tuần
                };
                schedules[5, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = allSubjects[fridayPattern[classIndex, 1]], // Slot 2
                    Count = 2 // slot thứ 2 trong tuần
                };
            }

            return schedules;
        }

        public CurriculumSubjectWithCount[,,] SortSubjectFourClassFlexibleSubject(List<CurriculumSubject> allSubjects, int numClasses = 4)
        {
            // Lọc môn học theo teaching mode
            var twoSlotSubjects = allSubjects.Where(s => s.TeachingMode == "ON/OFF").ToList();
            var oneSlotSubjects = allSubjects.Where(s => s.TeachingMode == "C-ON").ToList();

            // Kiểm tra đầu vào tối thiểu
            if (twoSlotSubjects.Count < 2)
                throw new ArgumentException("Cần ít nhất 2 môn học ON/OFF để phân phối lịch.");

            // Tạo lịch: [Ngày (0-7), Lớp, Slot (0,1)]
            var schedule = new CurriculumSubjectWithCount[8, numClasses, 2];

            // --- TẠO PATTERN CHO THỨ 2 ---
            var mondayPattern = GenerateFlexiblePattern(numClasses, twoSlotSubjects.Count);

            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[1, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = twoSlotSubjects[mondayPattern[classIndex][0]],
                    Count = 1
                };

                schedule[1, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = twoSlotSubjects[mondayPattern[classIndex][1]],
                    Count = 1
                };
            }

            // --- TẠO PATTERN CHO THỨ 3 BẰNG CÁCH XOAY PATTERN CŨ ---
            var tuesdayPattern = RotatePattern(mondayPattern, 2, twoSlotSubjects.Count);

            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[2, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = twoSlotSubjects[tuesdayPattern[classIndex][0]],
                    Count = 1
                };

                schedule[2, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = twoSlotSubjects[tuesdayPattern[classIndex][1]],
                    Count = 1
                };
            }

            // --- THỨ 4: ĐẢO SLOT CỦA THỨ 2 ---
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[3, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = schedule[1, classIndex, 1].Subject,
                    Count = 2
                };

                schedule[3, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = schedule[1, classIndex, 0].Subject,
                    Count = 2
                };
            }

            // --- THỨ 5: ĐẢO SLOT CỦA THỨ 3 ---
            for (int classIndex = 0; classIndex < numClasses; classIndex++)
            {
                schedule[4, classIndex, 0] = new CurriculumSubjectWithCount
                {
                    Subject = schedule[2, classIndex, 1].Subject,
                    Count = 2
                };

                schedule[4, classIndex, 1] = new CurriculumSubjectWithCount
                {
                    Subject = schedule[2, classIndex, 0].Subject,
                    Count = 2
                };
            }

            // --- PHÂN MÔN 1 SLOT CHO THỨ 6 & THỨ 7 ---
            (int day, int slot)[] fifthCourseSlots = new (int, int)[]
            {
            (5, 0), // Thứ 6, Slot 1
            (5, 1), // Thứ 6, Slot 2
            (6, 0), // Thứ 7, Slot 1
            (6, 1)  // Thứ 7, Slot 2
            };

            //for (int classIndex = 0; classIndex < numClasses; classIndex++)
            //{
            //    var (day, slot) = fifthCourseSlots[classIndex];
            //    schedule[day, classIndex, slot] = new CurriculumSubjectWithCount
            //    {
            //        Subject = oneSlotSubjects[0],
            //        Count = 1 // slot thứ 1 trong tuần
            //    };
            //}

            return schedule;
        }

        private List<int[]> GenerateFlexiblePattern(int numClasses, int numSubjects)
        {
            var pattern = new List<int[]>();

            for (int i = 0; i < numClasses; i++)
            {
                int first = i % numSubjects;
                int second = (i + 1) % numSubjects;

                // Tránh trùng slot
                if (first == second)
                    second = (second + 1) % numSubjects;

                pattern.Add(new int[] { first, second });
            }

            return pattern;
        }

        private List<int[]> RotatePattern(List<int[]> originalPattern, int offset, int numSubjects)
        {
            var newPattern = new List<int[]>();

            for (int i = 0; i < originalPattern.Count; i++)
            {
                int[] old = originalPattern[(i + offset) % originalPattern.Count];

                int rotated0 = (old[0]) % numSubjects;
                int rotated1 = (old[1]) % numSubjects;

                newPattern.Add(new int[] { rotated0, rotated1 });
            }

            return newPattern;
        }


    }
}
