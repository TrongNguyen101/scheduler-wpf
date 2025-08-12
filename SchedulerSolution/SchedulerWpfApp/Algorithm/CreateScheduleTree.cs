using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;

namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleTree
    {
        private readonly IScheduleServices _scheduleServices;

        private readonly Version2CreateSchedule _version2CreateSchedule;

        public CreateScheduleTree( IScheduleServices scheduleServices, Version2CreateSchedule version2CreateSchedule)
        {
            _scheduleServices = scheduleServices;
            _version2CreateSchedule = version2CreateSchedule;
        }

        public async Task<List<Schedule>> GenerateSchedules(Progress<int> progress, DateTime startDate, List<string> listMajorGroupA, List<string> listMajorGroupB)
        {
            //var listMajorGroupA = new List<string> { "FN", "HM", "MC", "BA", "TM", "IB", "EC", };
            //var listMajorGroupB = new List<string> { "AI", "SE", "AI", "JL", "KR", "EL" };
            //DateTime startDate = new DateTime(2025, 01, 06);
            
            //Comment 
            List<Schedule> schedulesTest = new List<Schedule>();

            //schedulesTest = await _reportV1.GenerateSchedules(startDate, listMajorGroupA, listMajorGroupB);

            schedulesTest = await _version2CreateSchedule.GenerateSchedules(startDate, listMajorGroupA, listMajorGroupB, progress);

            await _scheduleServices.AddScheduleAsync(schedulesTest, progress);

            return schedulesTest;
        }
    }
}