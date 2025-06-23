using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;

namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleTree
    {

        private readonly GenerateScheduleForAllDate _generateScheduleForAllDate;

        private readonly IScheduleServices _implementScheduleServices;
        private readonly ILecturerSubjectServices _implementLecturerSubjectServices;
        private readonly ICurriculumSubjectServices _curriculumSubjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly ILecturerServices _lecturerServices;


        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate,
                                IScheduleServices implementSchedule,
                                ILecturerSubjectServices implementLecturerSubjectServices,
                                ILecturerServices lecturerServices,
                                IGroupNameService groupNameService,
                                ICurriculumSubjectServices curriculumSubjectServices)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _implementScheduleServices = implementSchedule;
            _implementLecturerSubjectServices = implementLecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameService;
            _curriculumSubjectServices = curriculumSubjectServices;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = await _lecturerServices.GetAllLecturerAsync();
            List<CurriculumSubject> curriculumSubjects = await _curriculumSubjectServices.GetAllCurriculumSubjectAsync();
            List<GroupClass> listGroupName = await _groupNameService.GetAllAsync();
            List<LecturerSubject> lecturerSubjects = await _implementLecturerSubjectServices.GetAllAsync();
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            allSchedules = await _generateScheduleForAllDate.CreateSchedules(curriculumSubjects, listGroupName, lecturerSubjects, startDate, lecturerRequests);
            await _implementScheduleServices.AddScheduleAsync(allSchedules);

            return allSchedules;
        }
    }
}