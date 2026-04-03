using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IClassRepository
    {
        Task<IEnumerable<Class>> GetClassesByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken);
        Task<Class> CreateClassAsync(Class entity, CancellationToken cancellationToken);
        Task<Class?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateClassAsync(Class entity, CancellationToken cancellationToken);
        Task<bool> DeleteClassAsync(int id, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Class>> GetAllClassesAsync(CancellationToken cancellationToken);
        Task<Dictionary<(int GroupId, int DisciplineId), IEnumerable<Class>>> GetClassesByGroupIdsAndDisciplineIdsAsync(
            List<int> groupIds, List<int> disciplineIds, CancellationToken cancellationToken);
    }

    public class ClassRepository(AppDbContext dbContext, IDbContextFactory<AppDbContext> contextFactory) : IClassRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Class> CreateClassAsync(Class entity, CancellationToken cancellationToken)
        {
            await _dbContext.Classes.AddAsync(entity, cancellationToken: cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await _dbContext.Classes.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteClassAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _dbContext.Classes
                .Where(c => c.Id == id)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task<Class?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.Classes.FindAsync([id], cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Class>> GetClassesByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Classes
                .Where(c => c.GroupId == groupId && c.DisciplineId == disciplineId)
                .OrderBy(c => c.Weeknumber)
                .ThenBy(c => c.WeekDay)
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<bool> UpdateClassAsync(Class entity, CancellationToken cancellationToken)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Class>> GetAllClassesAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Classes.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<Dictionary<(int GroupId, int DisciplineId), IEnumerable<Class>>> GetClassesByGroupIdsAndDisciplineIdsAsync(
            List<int> groupIds, List<int> disciplineIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var classes = await context.Classes
                .Where(c => groupIds.Contains(c.GroupId) && disciplineIds.Contains(c.DisciplineId))
                .ToListAsync(cancellationToken);

            return classes
                .GroupBy(c => (c.GroupId, c.DisciplineId))
                .ToDictionary(
                    g => g.Key,
                    g => g.AsEnumerable());
        }
    }
}
