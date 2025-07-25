using SchedulerWpfApp.Model;
using SchedulerWpfApp.ServiceRefactor.ScheduleServices;
using SchedulerWpfApp.Algorithm.ReportVersion;

namespace SchedulerWpfApp.Algorithm
{
    public class CreateScheduleTree
    {
        private readonly IScheduleServices _scheduleServices;

        private readonly ReportV1 _reportV1;

        public CreateScheduleTree( IScheduleServices scheduleServices, ReportV1 reportV1)
        {
            _scheduleServices = scheduleServices;
            _reportV1 = reportV1;
        }

        public async Task<List<Schedule>> GenerateSchedules()
        {
            var listMajorGroupA = new List<string> { "FN", "HM", "MC", "BA", "TM", "IB", "EC", };
            var listMajorGroupB = new List<string> { "AI", "SE", "AI", "JL", "KR", "EL" };
            
            List<Schedule> schedulesTest = new List<Schedule>();
            DateTime startDate = new DateTime(2025, 01, 06);

            schedulesTest = await _reportV1.GenerateSchedules(startDate, listMajorGroupA, listMajorGroupB);

            await _scheduleServices.AddScheduleAsync(schedulesTest);

            return schedulesTest;
        }
    }
}