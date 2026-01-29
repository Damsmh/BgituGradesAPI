using BgutuGrades.Data;
using BgutuGrades.DTO;
using BgutuGrades.Models.Group;
using Grades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgutuGrades.Repositories
{
    public interface IGroupRepository {
        Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId);
        Task<Group> CreateGroupAsync(Group entity);
        Task<Group?> GetByIdAsync(int id);
        Task<bool> UpdateGroupAsync(Group entity);
        Task<bool> DeleteGroupAsync(int id);
        Task DeleteAllAsync();
    }

    public class GroupRepository(AppDbContext dbContext) : IGroupRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        public async Task<Group> CreateGroupAsync(Group entity)
        {
            await _dbContext.Groups.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Groups.ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Groups.Remove(entity);
            return true;
        }

        public async Task<Group?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Groups.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId)
        {
            var entities = await _dbContext.Groups
                                .Where(g => g.Classes.Any(c => c.DisciplineId == disciplineId))
                                .AsNoTracking()
                                .ToListAsync();
            return entities;
        }

        public async Task<bool> UpdateGroupAsync(Group entity)
        {
            _dbContext.Groups.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
