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

        private readonly Version2CreateSchedule _version2CreateSchedule;


        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate,
                                Version2CreateSchedule version2CreateSchedule,
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
            _version2CreateSchedule = version2CreateSchedule;
        }

        public async Task<List<Schedule>> GenerateSchedules(DateTime startDate)
        {
            _version2CreateSchedule.GenerateSchedules(startDate);

            //var listMajorA = new List<string> { "FN", "HM", "MC", "BA", "TM" };
            //var listMajorB = new List<string> { "AI", "SE", "AI", "JL", "KR", "EL" };


            //var listMajorOnOff = new List<string>();
            //listMajorOnOff.AddRange(listMajorA);
            //listMajorOnOff.AddRange(listMajorB);

            //List<Schedule> allSchedules = new List<Schedule>();
            //List<Lecturer> lecturers = await _lecturerServices.GetAllLecturerAsync();
            //List<CurriculumSubject> curriculumSubjects = await _curriculumSubjectServices.GetAllCurriculumSubjectAsync();
            //List<GroupClass> listGroupName = await _groupNameService.GetAllAsync();
            //List<LecturerSubject> lecturerSubjects = await _lecturerSubjectServices.GetAllAsync();
            //List<LecturerRequest> lecturerRequests = new List<LecturerRequest>();

            //List<GroupClass> listGroupNameBITAndBBAAndNN = listGroupName.Where(g => g.Department == "BIT" || g.Department == "BBA" || g.Department == "NN").ToList();

            //List<Schedule> schedulesForBITAndBBAAndNN = new List<Schedule>();

            //schedulesForBITAndBBAAndNN = await _generateScheduleForAllDate.CreateSchedules(curriculumSubjects, listGroupNameBITAndBBAAndNN, lecturerSubjects, startDate, lecturerRequests);

            //allSchedules.AddRange(schedulesForBITAndBBAAndNN);
             List<Schedule> schedulesTest = new List<Schedule>();

            schedulesTest = await _version2CreateSchedule.GenerateSchedules(startDate);

            //await _scheduleServices.AddScheduleAsync(schedulesTest);

            return schedulesTest;
        }
    }
}