using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IWorkRepository
    {
        Task<IEnumerable<Work>> GetAllWorksAsync();
        Task<Work> CreateWorkAsync(Work entity);
        Task<Work?> GetByIdAsync(int id);
        Task<IEnumerable<Work>> GetByDisciplineAndGroupAsync(int disciplineId, int groupId);
        Task<bool> UpdateWorkAsync(Work entity);
        Task<bool> DeleteWorkAsync(int id);
        Task DeleteAllAsync();
    }

    public class WorkRepository(AppDbContext dbContext) : IWorkRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Work> CreateWorkAsync(Work entity)
        {
            await _dbContext.Works.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteWorkAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Works.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Work?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Works.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Work>> GetAllWorksAsync()
        {
            var entities = await _dbContext.Works
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Work>> GetByDisciplineAndGroupAsync(int disciplineId, int groupId)
        {
            var entities = await _dbContext.Works
                .Where(w => w.DisciplineId == disciplineId &&
                            _dbContext.Classes.Any(c => c.DisciplineId == disciplineId &&
                                                        c.GroupId == groupId))
                .AsNoTracking()
                .ToListAsync();

            return entities;
        }

        public async Task<bool> UpdateWorkAsync(Work entity)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Works.ExecuteDeleteAsync();
        }
    }

}
