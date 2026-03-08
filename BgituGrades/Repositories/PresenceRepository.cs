using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BgituGrades.Repositories
{
    public interface IPresenceRepository
    {
        Task<IEnumerable<Presence>> GetAllPresencesAsync();
        Task<Presence> CreatePresenceAsync(Presence entity);
        Task<IEnumerable<Presence>> GetPresencesByDisciplineAndGroupAsync(int disciplineId, int groupId);
        Task<IEnumerable<Presence>> GetPresencesByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds);
        Task<Presence?> GetAsync(int disciplineId, int studentId, DateOnly date);
        Task<bool> DeletePresenceByStudentAndDateAsync(int studentId, DateOnly date);
        Task UpdatePresenceAsync(Presence entity);
        Task DeleteAllAsync();
        Task AddNewStudentPresences(int studentId, Dictionary<int, IEnumerable<DateOnly>> disciplines);
        Task<Presence?> GetPresenceByIdAsync(int id);
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
            var result = await _dbContext.Presences
                .Where(p => p.StudentId == studentId && p.Date == date)
                .ExecuteDeleteAsync();
            return result > 0;
        }

        public async Task UpdatePresenceAsync(Presence entity)
        {
            _dbContext.Presences.Update(entity);
            await _dbContext.SaveChangesAsync();
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

        public async Task<IEnumerable<Presence>> GetPresencesByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds)
        {
            var entities = await _dbContext.Presences
                .Where(p => disciplineIds.Contains(p.DisciplineId) &&
                           groupIds.Contains(p.Student.GroupId))
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task AddNewStudentPresences(int studentId, Dictionary<int, IEnumerable<DateOnly>> disciplines)
        {
            var presences = new List<Presence>();
            foreach (var discipline in disciplines)
            {
                foreach (var date in discipline.Value)
                {
                    presences.Add(new Presence
                    {
                        StudentId = studentId,
                        DisciplineId = discipline.Key,
                        Date = date,
                        IsPresent = PresenceType.PRESENT
                    });
                }
            }

            await _dbContext.Presences.AddRangeAsync(presences);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Presence?> GetPresenceByIdAsync(int id)
        {
            return await _dbContext.Presences.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
