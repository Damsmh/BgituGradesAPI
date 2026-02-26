using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.DTO;
using BgituGrades.Models.Group;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IGroupRepository {
        Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId);
        Task<IEnumerable<Group>> GetAllAsync();
        Task<Group> CreateGroupAsync(Group entity);
        Task<Group?> GetByIdAsync(int id);
        Task<bool> UpdateGroupAsync(Group entity);
        Task<bool> DeleteGroupAsync(int id);
        Task DeleteAllAsync();
        Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds);
    }

    public class GroupRepository(IDbContextFactory<AppDbContext> contextFactory) : IGroupRepository
    {
        public async Task<Group> CreateGroupAsync(Group entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Groups.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Groups.ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entity = await GetByIdAsync(id);
            context.Groups.Remove(entity);
            return true;
        }

        public async Task<IEnumerable<Group>> GetAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var groups = await context.Groups
                .Include(g => g.Classes)
                .ThenInclude(c => c.Discipline)
                .AsNoTracking().ToListAsync();
            return groups;
        }

        public async Task<Group?> GetByIdAsync(int id)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.Groups.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entities = await context.Groups
                                .Where(g => g.Classes.Any(c => c.DisciplineId == disciplineId))
                                .AsNoTracking()
                                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Groups
                .Include(g => g.Classes)
                    .ThenInclude(c => c.Discipline)
                .AsNoTracking()
                .Where(g => groupIds.Contains(g.Id))
                .ToListAsync();
        }

        public async Task<bool> UpdateGroupAsync(Group entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            context.Groups.Update(entity);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
