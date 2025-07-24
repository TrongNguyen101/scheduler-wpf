using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.GroupNameService;
using SchedulerWpfApp.ServiceRefactor.LecturerServices;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using SchedulerWpfApp.ServiceRefactor.LecturerSubjectServices;
using SchedulerWpfApp.ServiceRefactor.CurriculumSubjectServices;
using SchedulerWpfApp.Algorithm.ReportVersion;

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
        private readonly ReportV1 _reportV1;


        public CreateScheduleTree(GenerateScheduleForAllDate generateScheduleForAllDate,
                                Version2CreateSchedule version2CreateSchedule,
                                IScheduleServices scheduleServices,
                                ILecturerSubjectServices lecturerSubjectServices,
                                ILecturerServices lecturerServices,
                                IGroupNameService groupNameServices,
                                ICurriculumSubjectServices curriculumSubjectServices,
                                ReportV1 reportV1)
        {
            _generateScheduleForAllDate = generateScheduleForAllDate;
            _scheduleServices = scheduleServices;
            _lecturerSubjectServices = lecturerSubjectServices;
            _lecturerServices = lecturerServices;
            _groupNameService = groupNameServices;
            _curriculumSubjectServices = curriculumSubjectServices;
            _version2CreateSchedule = version2CreateSchedule;
            _reportV1 = reportV1;
        }

        public async Task<List<Schedule>> GenerateSchedules()
        {
            var listMajorGroupA = new List<string> { "FN", "HM", "MC", "BA", "TM", "IB", "EC", };
            var listMajorGroupB = new List<string> { "AI", "SE", "AI", "JL", "KR", "EL" };
            
            List<Schedule> schedulesTest = new List<Schedule>();
            DateTime startDate = new DateTime(2025, 01, 06);
            //schedulesTest = await _version2CreateSchedule.GenerateSchedules(startDate, listMajorGroupA, listMajorGroupB);

            schedulesTest = await _reportV1.GenerateSchedules(startDate, listMajorGroupA, listMajorGroupB);

            await _scheduleServices.AddScheduleAsync(schedulesTest);

            return schedulesTest;
        }
    }
}