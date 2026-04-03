using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IMarkRepository
    {
        Task<IEnumerable<Mark>> GetAllMarksAsync(CancellationToken cancellationToken);
        Task<Mark> CreateMarkAsync(Mark entity, CancellationToken cancellationToken);
        Task<IEnumerable<Mark>> GetMarksByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken);
        Task<bool> UpdateMarkAsync(Mark entity, CancellationToken cancellationToken);
        Task<bool> DeleteMarkByStudentAndWorkAsync(int studentId, int workId, CancellationToken cancellationToken);
        Task<Mark?> GetMarkByStudentAndWorkAsync(int studentId, int workId, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
        Task<double> GetAverageMarkByStudentAndDisciplineAsync(int studentId, int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<Mark>> GetMarksByDisciplinesAndGroupsAsync(List<int> disciplinesIds, List<int> groupsIds, CancellationToken cancellationToken);
        Task<Mark?> GetMarkByIdAsync(int id, CancellationToken cancellationToken);
    }

    public class MarkRepository(IDbContextFactory<AppDbContext> contextFactory) : IMarkRepository
    {

        public async Task<Mark> CreateMarkAsync(Mark entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Marks.AddAsync(entity, cancellationToken: cancellationToken);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<Mark>> GetAllMarksAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Marks
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<IEnumerable<Mark>> GetMarksByDisciplineAndGroupAsync(int disciplineId, int groupId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Marks
                .Where(m => m.Work!.DisciplineId == disciplineId &&
                           m.Student!.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }



        public async Task<bool> UpdateMarkAsync(Mark entity, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            context.Update(entity);
            await context.SaveChangesAsync(cancellationToken: cancellationToken);
            return true;
        }

        public async Task<bool> DeleteMarkByStudentAndWorkAsync(int studentId, int workId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var result = await context.Marks
                .Where(m => m.StudentId == studentId && m.WorkId == workId)
                .ExecuteDeleteAsync(cancellationToken: cancellationToken);
            return result > 0;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            await context.Marks.ExecuteDeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task<Mark?> GetMarkByStudentAndWorkAsync(int studentId, int workId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Marks
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.WorkId == workId, cancellationToken: cancellationToken);
        }

        public async Task<double> GetAverageMarkByStudentAndDisciplineAsync(int studentId, int disciplineId, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var marks = await context.Marks
                .Where(m => m.StudentId == studentId && m.Work!.DisciplineId == disciplineId)
                .Select(m => m.Value)
                .ToListAsync(cancellationToken: cancellationToken);

            var validMarks = marks
                .Where(m => double.TryParse(m, out _))
                .Select(double.Parse!)
                .ToList();

            return validMarks.Count > 0 ? validMarks.Average() : 0;
        }

        public async Task<IEnumerable<Mark>> GetMarksByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            var entities = await context.Marks
                .Where(m => disciplineIds.Contains(m.Work!.DisciplineId) && groupIds.Contains(m.Student!.GroupId))
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<Mark?> GetMarkByIdAsync(int id, CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken: cancellationToken);
            return await context.Marks.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken: cancellationToken);
        }
    }

}
