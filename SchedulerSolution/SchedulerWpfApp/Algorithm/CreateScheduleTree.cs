using SchedulerWpfApp.Model;
using SchedulerWpfApp.Services;
using SchedulerWpfApp.Services.LecturerSubjectServices;
using SchedulerWpfApp.Services.ScheduleServices;

namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleTree
    {

        private readonly GenerateScheduleForAllDate _generateScheduleForAllDate;

        private readonly InterfaceScheduleServices _implementScheduleServices;
        private readonly InterfaceLecturerSubjectServices _implementLecturerSubjectServices;
        private readonly InterfaceLecturerServices _implementLecturerServices;
        private readonly ISubjectServices _subjectServices;

        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate, InterfaceScheduleServices implementSchedule, InterfaceLecturerSubjectServices implementLecturerSubjectServices, ISubjectServices subjectServices, InterfaceLecturerServices implementLecturerServices)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _implementScheduleServices = implementSchedule;
            _implementLecturerSubjectServices = implementLecturerSubjectServices;
            _implementLecturerServices = implementLecturerServices;
            _subjectServices = subjectServices;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            /*Cần hàm đọc số lượng lớp ở đây*/
            int numberOfClasss = 4; // Total number of classes

            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = await _implementLecturerServices.GetAllLecturerAsync();
            List<Subject> subjects = await _subjectServices.GetAllAsync();
            List<LecturerSubject> lecturerSubjects = await _implementLecturerSubjectServices.GetAllLecturerSubjectAsync();
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            _generateScheduleForAllDate.CreateSchedules(allSchedules, subjects, numberOfClasss, lecturerSubjects, startDate, lecturerRequests);
            //_implementScheduleServices.AddScheduleAsync(allSchedules);

            return allSchedules;
        }
    }
}
