using SchedulerWpfApp.Repository.CurriculumRepository;
using SchedulerWpfApp.Repository.CurriculumSubjectsRepository;
using SchedulerWpfApp.Repository.GroupNameRepository;
using SchedulerWpfApp.Repository.LecturerRepository;
using SchedulerWpfApp.Repository.LecturerRequestRepository;
using SchedulerWpfApp.Repository.LectureSubjectRepository;
using SchedulerWpfApp.Repository.RoomRepository;
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
