using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.Services.ScheduleServices;
using SchedulerWpfApp.Services.LecturerSubjectServices;

namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleTree
    {

        private readonly GenerateScheduleForAllDate _generateScheduleForAllDate;

        private readonly InterfaceScheduleServices _implementScheduleServices;
        private readonly InterfaceLecturerSubjectServices _implementLecturerSubjectServices;
        private readonly InterfaceLecturerServices _implementLecturerServices;

        // services refactor
        private readonly ISubjectServices _subjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly ILecturerServices _lecturerService;


        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate,
                                InterfaceScheduleServices implementSchedule,
                                InterfaceLecturerSubjectServices implementLecturerSubjectServices,
                                ISubjectServices subjectServices,
                                ILecturerServices lecturerServices,
                                IGroupNameService groupNameService)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _implementScheduleServices = implementSchedule;
            _implementLecturerSubjectServices = implementLecturerSubjectServices;
            _lecturerService = lecturerServices;
            _subjectServices = subjectServices;
            _groupNameService = groupNameService;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = await _implementLecturerServices.GetAllLecturerAsync();
            List<Subject> subjects = await _subjectServices.GetAllAsync();
            List<GroupClass> listGroupName = await _groupNameService.GetAllAsync();
            List<LecturerSubject> lecturerSubjects = await _implementLecturerSubjectServices.GetAllLecturerSubjectAsync();
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            allSchedules = await _generateScheduleForAllDate.CreateSchedules(subjects, listGroupName, lecturerSubjects, startDate, lecturerRequests);
            //_implementScheduleServices.AddScheduleAsync(allSchedules);

            return allSchedules;
        }
    }
}