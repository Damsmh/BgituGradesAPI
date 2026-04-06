using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Models.Class;
using BgituGrades.Models.Mark;
using BgituGrades.Models.Presence;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllStudentsAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Student>> GetStudentsByGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<IEnumerable<Student>> GetStudentsByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken);
        Task<IEnumerable<FullGradeMarkResponse>> GetMarksGrade(IEnumerable<Work> works, int groupId, int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<FullGradePresenceResponse>> GetPresenseGrade(IEnumerable<ClassDateResponse> scheduleDates, int groupId, int disciplineId, CancellationToken cancellationToken);
        Task<Student> CreateStudentAsync(Student entity, CancellationToken cancellationToken);
        Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStudentAsync(Student entity, CancellationToken cancellationToken);
        Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<Student>> GetArchivedByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken);
        Task<IEnumerable<Student>> GetStudentsByIdsAsync(int[] studentIds, CancellationToken cancellationToken);
        Task BulkInsertAsync(List<Student> students, CancellationToken cancellationToken);
        Task DeleteByIdsAsync(List<int> studentsIds, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
    }

    public class StudentRepository(IDbContextFactory<AppDbContext> contextFactory) : IStudentRepository
    {

        public async Task<Student> CreateStudentAsync(Student entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Students.AddAsync(entity, cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var result = await context.Students.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entity = await context.Students.FindAsync([id], cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Students
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Student>> GetStudentsByGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Students
                .Where(s => s.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<FullGradePresenceResponse>> GetPresenseGrade(IEnumerable<ClassDateResponse> scheduleDates,
            int groupId, int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var studentsWithPresence = await context.Students
                .Where(s => s.GroupId == groupId)
                    .Include(s => s.Presences)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken: cancellationToken);

            var presenceByStudent = studentsWithPresence.Select(s => new
            {
                s.Id,
                s.Name,
                PresencesByDate = s.Presences!
                    .Where(p => p.DisciplineId == disciplineId)
                    .ToLookup(p => (p.ClassId, p.Date), p => p.IsPresent)
            }).ToList();

            var scheduleDatesList = scheduleDates.ToList();
            var result = presenceByStudent.Select(s => new FullGradePresenceResponse
            {
                StudentId = s.Id,
                Name = s.Name,
                Presences = scheduleDatesList.Select(date => new GradePresenceResponse
                {
                    ClassId = date.Id,
                    ClassType = date.ClassType,
                    Date = date.Date,
                    IsPresent = s.PresencesByDate[(date.Id, date.Date)].FirstOrDefault(PresenceType.PRESENT)
                }).ToList()
            });

            return result;
        }

        public async Task<IEnumerable<FullGradeMarkResponse>> GetMarksGrade(IEnumerable<Work> works,
            int groupId, int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var studentsWithMarks = await context.Students
                .Where(s => s.GroupId == groupId)
                    .Include(s => s.Marks!)
                        .ThenInclude(m => m.Work)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken: cancellationToken);

            var marksByStudent = studentsWithMarks.Select(s => new
            {
                s.Id,
                s.Name,
                MarksByWorkId = s.Marks!
                    .Where(m => m.Work!.DisciplineId == disciplineId)
                    .ToLookup(m => m.WorkId, m => m.Value)
            }).ToList();

            var worksList = works.ToList();
            var result = marksByStudent.Select(s => new FullGradeMarkResponse
            {
                StudentId = s.Id,
                Name = s.Name,
                Marks = worksList.Select(work => new GradeMarkResponse
                {
                    WorkId = work.Id,
                    Name = work.Name!,
                    Value = s.MarksByWorkId[work.Id].FirstOrDefault() ?? ""
                }).ToList()
            });

            return result;
        }

        public async Task<bool> UpdateStudentAsync(Student entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            context.Update(entity);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Student>> GetStudentsByIdsAsync(int[] studentIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Students
                .AsNoTracking()
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetStudentsByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Students
                .AsNoTracking()
                .Where(s => groupIds.Contains(s.GroupId))
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task BulkInsertAsync(List<Student> students, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var bulkConfig = new BulkConfig { UpdateByProperties = [nameof(Student.OfficialId), nameof(Student.GroupId)] };
            await context.BulkInsertOrUpdateAsync(students, bulkConfig, cancellationToken: cancellationToken);
        }



        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Students.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<Student>> GetArchivedByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var archivedStudents = await context.ReportSnapshots
                .AsNoTracking()
                .Where(r => groupIds.Contains(r.GroupId))
                .Select(r => new
                {
                    r.StudentId,
                    r.StudentName
                })
                .Distinct()
                .Select(r => new Student { Id = r.StudentId, Name = r.StudentName })
                .ToListAsync(cancellationToken: cancellationToken);
            return archivedStudents;
        }

        public async Task DeleteByIdsAsync(List<int> studentsIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Students.Where(s => studentsIds.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }
    }
}