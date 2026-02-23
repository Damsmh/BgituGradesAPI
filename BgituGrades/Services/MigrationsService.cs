using BgituGrades.Repositories;
using BgituGrades.Models.Mark;

namespace BgituGrades.Services
{
    public interface IMigrationService
    {
        Task DeleteAll();
    }
    public class MigrationsService(IClassRepository classRepository, IDisciplineRepository disciplineRepository,
        IGroupRepository groupRepository, IMarkRepository markRepository, 
        IPresenceRepository presenceRepository, ITransferRepository transferRepository, IWorkRepository workRepository) : IMigrationService
    {
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly ITransferRepository _transferRepository = transferRepository;
        private readonly IWorkRepository _workRepository = workRepository;
        private readonly IMarkRepository _markRepository = markRepository;
        public async Task DeleteAll()
        {
            await _markRepository.DeleteAllAsync();
            await _classRepository.DeleteAllAsync();
            await _disciplineRepository.DeleteAllAsync();
            await _groupRepository.DeleteAllAsync();
            await _presenceRepository.DeleteAllAsync();
            await _transferRepository.DeleteAllAsync();
            await _workRepository.DeleteAllAsync();
        }
    }
}
