using SchedulerWpfApp.Data;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Repository.LecturerRequestRepository
{
    public class LecturerRequestRepository : BaseRepository<LecturerRequest>, ILecturerRequestRepository
    {
        #region Constructors
        public LecturerRequestRepository(DataContext context) : base(context) { }
        #endregion
    }
}
