using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IPresenceRepository
    {
        Task<IEnumerable<Presence>> GetAllPresencesAsync();
        Task<Presence> CreatePresenceAsync(Presence entity);
        Task<IEnumerable<Presence>> GetPresencesByDisciplineAndGroupAsync(int disciplineId, int groupId);
        Task<Presence?> GetAsync(int disciplineId, int studentId, DateOnly date);
        Task<bool> DeletePresenceByStudentAndDateAsync(int studentId, DateOnly date);
        Task<bool> UpdatePresenceAsync(Presence entity);
        Task DeleteAllAsync();
    }

    public class PresenceRepository(AppDbContext dbContext) : IPresenceRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Presence> CreatePresenceAsync(Presence entity)
        {
            await _dbContext.Presences.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<Presence>> GetAllPresencesAsync()
        {
            var entities = await _dbContext.Presences
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Presence>> GetPresencesByDisciplineAndGroupAsync(int disciplineId, int groupId)
        {
            var entities = await _dbContext.Presences
                .Where(p => p.DisciplineId == disciplineId &&
                           p.Student.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<bool> DeletePresenceByStudentAndDateAsync(int studentId, DateOnly date)
        {
            var entity = await _dbContext.Presences
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.Date == date);
            _dbContext.Presences.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePresenceAsync(Presence entity)
        {
            _dbContext.Presences.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Presences.ExecuteDeleteAsync();
        }

        public async Task<Presence?> GetAsync(int disciplineId, int studentId, DateOnly date)
        {
            var presence = await _dbContext.Presences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.DisciplineId == disciplineId &&
                                         p.StudentId == studentId &&
                                         p.Date == date);
            return presence;
        }
    }
}
