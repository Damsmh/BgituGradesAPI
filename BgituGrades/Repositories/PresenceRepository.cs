using BgituGrades.Data;
using BgituGrades.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Text.RegularExpressions;

namespace BgituGrades.Repositories
{
    public interface IPresenceRepository
    {
        Task<IEnumerable<Presence>> GetAllPresencesAsync(CancellationToken cancellationToken);
        Task<Presence> CreatePresenceAsync(Presence entity, CancellationToken cancellationToken);
        Task<IEnumerable<Presence>> GetPresencesByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken);
        Task<IEnumerable<Presence>> GetPresencesByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds, CancellationToken cancellationToken);
        Task<Presence?> GetAsync(int disciplineId, int studentId, DateOnly date, CancellationToken cancellationToken);
        Task<bool> DeletePresenceByStudentAndDateAsync(int studentId, DateOnly date, CancellationToken cancellationToken);
        Task UpdatePresenceAsync(Presence entity, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task AddNewStudentPresences(int studentId, Dictionary<int, IEnumerable<DateOnly>> disciplines, CancellationToken cancellationToken);
        Task<Presence?> GetPresenceByIdAsync(int id, CancellationToken cancellationToken);
        Task BulkInsertPresencesAsync(Dictionary<int, Dictionary<int, IEnumerable<DateOnly>>> studentDisciplines, CancellationToken cancellationToken);
    }

    public class PresenceRepository(AppDbContext dbContext) : IPresenceRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Presence> CreatePresenceAsync(Presence entity, CancellationToken cancellationToken)
        {
            await _dbContext.Presences.AddAsync(entity, cancellationToken: cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Presence>> GetAllPresencesAsync(CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Presences
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Presence>> GetPresencesByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Presences
                .Where(p => p.DisciplineId == disciplineId &&
                           p.Student.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<bool> DeletePresenceByStudentAndDateAsync(int studentId, DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _dbContext.Presences
                .Where(p => p.StudentId == studentId && p.Date == date)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task UpdatePresenceAsync(Presence entity, CancellationToken cancellationToken)
        {
            _dbContext.Presences.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await _dbContext.Presences.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<Presence?> GetAsync(int disciplineId, int studentId, DateOnly date, CancellationToken cancellationToken)
        {
            var presence = await _dbContext.Presences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.DisciplineId == disciplineId &&
                                         p.StudentId == studentId &&
                                         p.Date == date, cancellationToken: cancellationToken);
            return presence;
        }

        public async Task<IEnumerable<Presence>> GetPresencesByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Presences
                .Where(p => disciplineIds.Contains(p.DisciplineId) &&
                           groupIds.Contains(p.Student.GroupId))
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task AddNewStudentPresences(int studentId, Dictionary<int, IEnumerable<DateOnly>> disciplines, CancellationToken cancellationToken)
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

            await _dbContext.Presences.AddRangeAsync(presences, cancellationToken: cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        public async Task<Presence?> GetPresenceByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Presences.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken: cancellationToken);
        }

        public async Task BulkInsertPresencesAsync(Dictionary<int, Dictionary<int, IEnumerable<DateOnly>>> studentDisciplines, 
            CancellationToken cancellationToken)
        {
            var presences = studentDisciplines
            .SelectMany(s => s.Value
                .SelectMany(d => d.Value.Select(date => new Presence
                {
                    StudentId = s.Key,
                    DisciplineId = d.Key,
                    Date = date,
                    IsPresent = PresenceType.PRESENT
                })))
            .ToList();

            await _dbContext.BulkInsertAsync(presences, cancellationToken: cancellationToken);
        }
    }
}
