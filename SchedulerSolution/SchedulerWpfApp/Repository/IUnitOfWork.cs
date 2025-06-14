using SchedulerWpfApp.Repository.CurriculumRepo;
using SchedulerWpfApp.Repository.CurriculumSubjectsRepo;
using SchedulerWpfApp.Repository.GroupNameRepo;
using SchedulerWpfApp.Repository.LecturerRepository;
using SchedulerWpfApp.Repository.LecturerRequestRepository;
using SchedulerWpfApp.Repository.LectureSubjectRepo;
using SchedulerWpfApp.Repository.RoomRepo;
using SchedulerWpfApp.Repository.ScheduleRepository;
using SchedulerWpfApp.Repository.SubjectRepository;
namespace SchedulerWpfApp.Repository
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IBaseRepository<T> Repository<T>() where T : class;
        IRoomRepository RoomRepository { get; }
        ILecturerRepository LecturerRepository { get; }
        ILectureSubjectRepository LectureSubjectRepository { get; }
        ILecturerRequestRepository LecturerRequestRepository { get; }
        IGroupNameRepository GroupNameRepository { get; }
        ICurriculumRepository CurriculumRepository { get; }
        ICurriculumSubjectsRepository CurriculumSubjectsRepository { get; }
        IScheduleRepository ScheduleRepository { get; }
        ISubjectRepository SubjectRepository { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
