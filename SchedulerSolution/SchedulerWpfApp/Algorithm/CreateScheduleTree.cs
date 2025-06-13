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
        private readonly IGroupNameService _groupNameService;

        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate, InterfaceScheduleServices implementSchedule, InterfaceLecturerSubjectServices implementLecturerSubjectServices, ISubjectServices subjectServices, InterfaceLecturerServices implementLecturerServices, IGroupNameServiceOld groupNameService)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _implementScheduleServices = implementSchedule;
            _implementLecturerSubjectServices = implementLecturerSubjectServices;
            _implementLecturerServices = implementLecturerServices;
            _subjectServices = subjectServices;
            _groupNameService = groupNameService;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            List<GroupClass> listGroupName = await _groupNameService.GetAllAsync();
            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = await _implementLecturerServices.GetAllLecturerAsync();
            List<Subject> subjects = await _subjectServices.GetAllAsync();
            List<LecturerSubject> lecturerSubjects = await _implementLecturerSubjectServices.GetAllLecturerSubjectAsync();
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            allSchedules = await _generateScheduleForAllDate.CreateSchedules(subjects, listGroupName, lecturerSubjects, startDate, lecturerRequests);
            //_implementScheduleServices.AddScheduleAsync(allSchedules);

            return allSchedules;
        }
    }
}
