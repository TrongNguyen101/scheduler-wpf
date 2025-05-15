using SchedulerWpfApp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulerWpfApp.Services
{
    public class CreateScheduleTree
    {

        private readonly GenerateScheduleForAllDate _generateScheduleForAllDate;
        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
        }

        public List<Schedule> GenerateSchedules(DateTime startDate)
        {
            int numberOfClasss = 4; // Total number of classes

            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = new List<Lecturer>();
            List<Subject> subjects = new List<Subject>();

            List<LecturerSubject> lecturerSubjects = new List<LecturerSubject>();
            lecturerSubjects.Add(new LecturerSubject("L1", "SWP391", "Nguyen Van Xoai", 1));
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            lecturerRequests.Add(new LecturerRequest(1, "L1", "Monday", "AM", null, null));
            lecturerRequests.Add(new LecturerRequest(1, "L1", "Wednesday", "AM", null, null));

            lecturerSubjects.Add(new LecturerSubject("L2", "SWP391", "Sờ Mai", 1));
            lecturerSubjects.Add(new LecturerSubject("L3", "SWP391", "Nguyen Mang Gồ", 1));
            lecturerSubjects.Add(new LecturerSubject("L4", "SWP391", "Nguyen Vỉa Hè", 1));

            lecturerSubjects.Add(new LecturerSubject("L5", "SWT301", "Nguyen Hoa Hong", 1));
            lecturerSubjects.Add(new LecturerSubject("L6", "SWT301", "Nguyen Thi Hoa", 1));
            lecturerSubjects.Add(new LecturerSubject("L7", "SWT301", "Nguyen    Thi Bưởi", 1));
            lecturerSubjects.Add(new LecturerSubject("L8", "SWT301", "Nguyen Thi Đào", 1));

            lecturerSubjects.Add(new LecturerSubject("L9", "SWR302", "Nguyen Thi Oi", 1));
            lecturerSubjects.Add(new LecturerSubject("L10", "SWR302", "Nguyen Thi Cam", 1));
            lecturerSubjects.Add(new LecturerSubject("L11", "SWR302", "Nguyen Thi Mit", 1));
            lecturerSubjects.Add(new LecturerSubject("L12", "SWR302", "Nguyen Thi Leo", 1));

            lecturerSubjects.Add(new LecturerSubject("L13", "PRN211", "Nguyen Thi Man", 1));
            lecturerSubjects.Add(new LecturerSubject("L14", "PRN211", "Nguyen Teo Em", 1));
            lecturerSubjects.Add(new LecturerSubject("L15", "PRN211", "Nguyen Thi Cam", 1));
            lecturerSubjects.Add(new LecturerSubject("L16", "PRN211", "Nguyen Thi Chuoi", 1));

            lecturerSubjects.Add(new LecturerSubject("L17", "ENW11", "Nguyen Thi Hoa", 1));

            subjects.Add(new Subject("SWP391", "Software Engineering", "SE", 4, 2, ""));
            subjects.Add(new Subject("SWT301", "Software Testing", "SE", 4, 2, ""));
            subjects.Add(new Subject("SWR302", "Software Requirement", "SE", 4, 2, ""));
            subjects.Add(new Subject("PRN211", "Programming", "SE", 4, 2, ""));
            subjects.Add(new Subject("ENW11", "English", "SE", 4, 1, ""));

            lecturers.Add(new Lecturer("L1", "Nguyen Van Xoai"));
            lecturers.Add(new Lecturer("L2", "Sờ Mai"));
            lecturers.Add(new Lecturer("L3", "Nguyen Mang Gồ"));
            lecturers.Add(new Lecturer("L4", "Nguyen Vỉa Hè"));

            lecturers.Add(new Lecturer("L5", "Nguyen Hoa Hong"));
            lecturers.Add(new Lecturer("L6", "Nguyen Thi Hoa"));
            lecturers.Add(new Lecturer("L7", "Nguyen Thi Bưởi"));
            lecturers.Add(new Lecturer("L8", "Nguyen Thi Đào"));

            lecturers.Add(new Lecturer("L9", "Nguyen Thi Oi"));
            lecturers.Add(new Lecturer("L10", "Nguyen Thi Cam"));
            lecturers.Add(new Lecturer("L11", "Nguyen Thi Mit"));
            lecturers.Add(new Lecturer("L12", "Nguyen Thi Leo"));

            lecturers.Add(new Lecturer("L13", "Nguyen Thi Man"));
            lecturers.Add(new Lecturer("L14", "Nguyen Teo Em"));
            lecturers.Add(new Lecturer("L15", "Nguyen Thi Cam"));
            lecturers.Add(new Lecturer("L16", "Nguyen Thi Chuoi"));

            lecturers.Add(new Lecturer("L17", "Nguyen Thi Hoa"));

            _generateScheduleForAllDate.CreateSchedules(allSchedules, subjects, numberOfClasss, lecturerSubjects, startDate, lecturerRequests);

            return allSchedules;
        }
    }
}
