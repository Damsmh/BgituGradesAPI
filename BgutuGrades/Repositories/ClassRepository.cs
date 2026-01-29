using BgutuGrades.Data;
using BgutuGrades.Entities;
using BgutuGrades.Models.Class;
using Microsoft.EntityFrameworkCore;

namespace BgutuGrades.Repositories
{
    public interface IClassRepository
    {
        Task<IEnumerable<Class>> GetClassesByDisciplineAndGroupAsync(int disciplineId, int groupId);
        Task<Class> CreateClassAsync(Class entity);
        Task<Class?> GetByIdAsync(int id);
        Task<bool> UpdateClassAsync(Class entity);
        Task<bool> DeleteClassAsync(int id);
        Task DeleteAllAsync();
    }

    public class ClassRepository(AppDbContext dbContext) : IClassRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Class> CreateClassAsync(Class entity)
        {
            await _dbContext.Classes.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Classes.ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteClassAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Classes.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Class?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Classes.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Class>> GetClassesByDisciplineAndGroupAsync(int disciplineId, int groupId)
        {
            var entities = await _dbContext.Classes
                .Where(c => c.GroupId == groupId && c.DisciplineId == disciplineId)
                .OrderBy(c => c.Weeknumber)
                .ThenBy(c => c.WeekDay)
                .ToListAsync();
            return entities;
        }

        public async Task<bool> UpdateClassAsync(Class entity)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
