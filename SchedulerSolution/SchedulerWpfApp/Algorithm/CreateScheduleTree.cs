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

        private readonly IScheduleServices _scheduleServices;
        private readonly ILecturerSubjectServices _lecturerSubjectServices;
        private readonly ICurriculumSubjectServices _curriculumSubjectServices;
        private readonly IGroupNameService _groupNameService;
        private readonly ILecturerServices _lecturerServices;


        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate,
                                IScheduleServices scheduleServices,
                                ILecturerSubjectServices lecturerSubjectServices,
                                ILecturerServices lecturerServices,
                                IGroupNameService groupNameServices,
                                ICurriculumSubjectServices curriculumSubjectServices)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _scheduleServices = scheduleServices;
            _lecturerSubjectServices = lecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameServices;
            _curriculumSubjectServices = curriculumSubjectServices;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {

            List<Schedule> allSchedules = new List<Schedule>();
            List<Lecturer> lecturers = await _lecturerServices.GetAllLecturerAsync();
            List<CurriculumSubject> curriculumSubjects = await _curriculumSubjectServices.GetAllCurriculumSubjectAsync();
            List<GroupClass> listGroupName = await _groupNameService.GetAllAsync();
            List<LecturerSubject> lecturerSubjects = await _lecturerSubjectServices.GetAllAsync();
            List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            List<GroupClass> listGroupNameBITAndBBA = listGroupName.Where(g => g.Department == "BIT" || g.Department == "BBA").ToList();

            List<Schedule> schedulesForBITAndBBA = new List<Schedule>();

            schedulesForBITAndBBA = await _generateScheduleForAllDate.CreateSchedules(curriculumSubjects, listGroupNameBITAndBBA, lecturerSubjects, startDate, lecturerRequests);
           
            allSchedules.AddRange(schedulesForBITAndBBA);

            await _scheduleServices.AddScheduleAsync(allSchedules);

            return allSchedules;
        }
    }
}