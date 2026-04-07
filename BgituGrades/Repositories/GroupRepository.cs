using AutoMapper;
using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Models.Group;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IGroupRepository
    {
        Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetAllAsync(CancellationToken cancellationToken);
        Task<Group> CreateGroupAsync(Group entity, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> CreateGroupAsync(IEnumerable<Group> entities, CancellationToken cancellationToken);
        Task<Group?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetArchivedByPeriod(int semester, int year, CancellationToken cancellationToken);
        Task<IEnumerable<CourseReponse>> GetCoursesAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetGroupsByCoursesAsync(IEnumerable<int> courses, CancellationToken cancellationToken);
        Task<bool> UpdateGroupAsync(Group entity, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int id, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds, CancellationToken cancellationToken);
        Task<IEnumerable<Group>> GetByIdsAsync(List<int> groupIds, CancellationToken cancellationToken);
        Task<IEnumerable<CourseReponse>> GetArchivedCoursesByPeriodAsync(int year, int semester, CancellationToken cancellationToken);
        Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAsync(IEnumerable<int> courses, CancellationToken cancellationToken);
        Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAndPeriodAsync(GetArchivedByCoursesRequest request, CancellationToken cancellationToken);
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

        public async Task<IEnumerable<Group>> CreateGroupAsync(IEnumerable<Group> entities, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entityList = entities
                .GroupBy(g => g.Name)
                .Select(g => g.First())
                .ToList();
            var bulkConfig = new BulkConfig { SetOutputIdentity = true };
            await context.BulkInsertAsync(entityList, bulkConfig, cancellationToken: cancellationToken);
            return entityList;
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
                .Include(g => g.Classes!)
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
                .Select(r => new { r.GroupId, r.GroupName })
                .Distinct()
                .Select(r => new Group { Id = r.GroupId, Name = r.GroupName })
                .ToListAsync(cancellationToken: cancellationToken);
            return archivedGroups;
        }

        public async Task<IEnumerable<CourseReponse>> GetArchivedCoursesByPeriodAsync(int year, int semester, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.ReportSnapshots
                .AsNoTracking()
                .Where(r => r.Year == year && r.Semester == semester)
                .Select(r => r.GroupCourseNumber)
                .Distinct()
                .Select(course => new CourseReponse { CourseNumber = course })
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAndPeriodAsync(
            GetArchivedByCoursesRequest request, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            int[] courses = request.Courses!.Values;

            return await context.ReportSnapshots
                .AsNoTracking()
                .Where(r => r.Year == request.Year
                         && r.Semester == request.Semester
                         && courses.Contains(r.GroupCourseNumber))
                .GroupBy(r => new { r.GroupId, r.GroupName })
                .Select(g => new ArchivedGroupResponse { Id = g.Key.GroupId, Name = g.Key.GroupName })
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAsync(IEnumerable<int> courses, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.ReportSnapshots
                .AsNoTracking()
                .Where(r => courses.Contains(r.GroupCourseNumber))
                .DistinctBy(r => r.GroupId)
                .Select(r => new ArchivedGroupResponse { Id = r.GroupId, Name = r.GroupName })
                .ToListAsync(cancellationToken);
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

        public async Task<IEnumerable<CourseReponse>> GetCoursesAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var courses = await context.Groups
                .AsNoTracking()
                .Select(g => g.CourseNumber)
                .Distinct()
                .Select(course => new CourseReponse { CourseNumber = course })
                .ToListAsync(cancellationToken);
            return courses;
        }

        public async Task<IEnumerable<Group>> GetGroupsByCoursesAsync(IEnumerable<int> courses, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Groups
                .Where(g => courses.Contains(g.CourseNumber))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Group>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Groups
                .Where(g => g.Classes!.Any(c => c.DisciplineId == disciplineId))
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Group>> GetGroupsByIdsAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Groups
                .Where(g => groupIds.Contains(g.Id))
                .Include(g => g.Classes!)
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
