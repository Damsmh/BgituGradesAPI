using BgutuGrades.Data;
using BgutuGrades.Models.Discipline;
using Grades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgutuGrades.Repositories
{
    public interface IDisciplineRepository {
        Task<IEnumerable<Discipline>> GetAllAsync();
        Task<Discipline> CreateDisciplineAsync(Discipline entity);
        Task<Discipline?> GetByIdAsync(int id);
        Task<bool> UpdateDisciplineAsync(Discipline entity);
        Task<bool> DeleteDisciplineAsync(int id);
        Task DeleteAllAsync();
    }

    public class DisciplineRepository(AppDbContext dbContext) : IDisciplineRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        public async Task<Discipline> CreateDisciplineAsync(Discipline entity)
        {
            await _dbContext.Disciplines.AddAsync(entity);
            await _dbContext.SaveChangesAsync();  
            return entity;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Disciplines.ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteDisciplineAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Disciplines.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Discipline>> GetAllAsync()
        {
            var entities = await _dbContext.Disciplines.AsNoTracking().ToListAsync();
            return entities;
        }

        public async Task<Discipline?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Disciplines.FindAsync(id);
            return entity;
        }

        public async Task<bool> UpdateDisciplineAsync(Discipline entity)
        {
            _dbContext.Disciplines.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
