using AutoMapper;
using SchedulerWpfApp.Model;
using SchedulerWpfApp.Repository;
using SchedulerWpfApp.ServiceRefactor.SubjectServices;

namespace SchedulerWpfApp.ServiceRefactor.SubjectSerivces
{
    public class SubjectServices :ISubjectServices
    {
        private readonly IMapper _mapper;
        public SubjectServices(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
        }
        //public async Task<Subject?> GetSubjectByCodeAsync(string code)
        //{
        //    await _unitOfWork.Repository<Subject>().GetSubjectByCodeAsync(code);
        //}
    }
}
