using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IWorkRepository
    {
        Task<IEnumerable<Work>> GetAllWorksAsync(CancellationToken cancellationToken);
        Task<Work> CreateWorkAsync(Work entity, CancellationToken cancellationToken);
        Task<Work?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<Work>> GetByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken);
        Task<bool> UpdateWorkAsync(Work entity, CancellationToken cancellationToken);
        Task<bool> DeleteWorkAsync(int id, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
    }

    public class WorkRepository(AppDbContext dbContext) : IWorkRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Work> CreateWorkAsync(Work entity, CancellationToken cancellationToken)
        {
            await _dbContext.Works.AddAsync(entity, cancellationToken: cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteWorkAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _dbContext.Works
                .Where(w => w.Id == id)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task<Work?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.Works.FindAsync([id], cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Work>> GetAllWorksAsync(CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Works
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Work>> GetByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Works
                .Where(w => w.DisciplineId == disciplineId && w.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);

            return entities;
        }

        public async Task<bool> UpdateWorkAsync(Work entity, CancellationToken cancellationToken)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await _dbContext.Works.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }
    }

}
