using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.DTO;
using BgituGrades.Models.Group;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using AutoMapper;

namespace BgituGrades.Repositories
{
    public interface IGroupRepository {
        Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken);
        Task<Group> CreateGroupAsync(Group entity, CancellationToken cancellationToken);
        Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetArchivedByPeriod(int semester, int year, CancellationToken cancellationToken);
        Task<bool> UpdateGroupAsync(Group entity, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int id, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetByIdsAsync(List<int> groupIds, CancellationToken cancellationToken);
    }

    public class GroupRepository(IDbContextFactory<AppDbContext> contextFactory, IMapper mapper) : IGroupRepository
    {
        private readonly IMapper _mapper = mapper;
        public async Task<Group> CreateGroupAsync(Group entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Groups.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Groups.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteGroupAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var result = await context.Groups
                .Where(g => g.Id == id)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var groups = await context.Groups
                .Include(g => g.Classes)
                    .ThenInclude(c => c.Discipline)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return groups;
        }

        public async Task<IEnumerable<Group>> GetArchivedByPeriod(int semester, int year, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var archivedGroups = await context.ReportSnapshots
                .AsNoTracking()
                .Where(r => r.Semester == semester && r.Year == year)
                .Select(r => new { r.GroupId, r.GroupName} )
                .Distinct()
                .Select(r => new Group { Id = r.GroupId, Name = r.GroupName })
                .ToListAsync(cancellationToken: cancellationToken);
            return _mapper.Map<List<Group>>(archivedGroups);
        }

        public async Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entity = await context.Groups.FindAsync([id], cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Group>> GetByIdsAsync(List<int> groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Groups
                .Where(g => groupIds.Contains(g.Id))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Groups
                .Where(g => g.Classes.Any(c => c.DisciplineId == disciplineId))
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Groups
                .Where(g => groupIds.Contains(g.Id))
                .Include(g => g.Classes)
                    .ThenInclude(c => c.Discipline)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<bool> UpdateGroupAsync(Group entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            context.Groups.Update(entity);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }
    }
}
