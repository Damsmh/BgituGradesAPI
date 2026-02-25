using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BgituGrades.Repositories
{
    public interface IMarkRepository
    {
        Task<IEnumerable<Mark>> GetAllMarksAsync();
        Task<Mark> CreateMarkAsync(Mark entity);
        Task<IEnumerable<Mark>> GetMarksByDisciplineAndGroupAsync(int disciplineId, int groupId);
        Task<bool> UpdateMarkAsync(Mark entity);
        Task<bool> DeleteMarkByStudentAndWorkAsync(int studentId, int workId);
        Task<Mark?> GetMarkByStudentAndWorkAsync(int studentId, int workId);
        Task DeleteAllAsync();
        Task<double> GetAverageMarkByStudentAndDisciplineAsync(int studentId, int disciplineId);
        Task<IEnumerable<Mark>> GetMarksByDisciplinesAndGroupsAsync(List<int> disciplinesIds, List<int> groupsIds);
    }

    public class MarkRepository(IDbContextFactory<AppDbContext> contextFactory) : IMarkRepository
    {

        public async Task<Mark> CreateMarkAsync(Mark entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Marks.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<Mark>> GetAllMarksAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entities = await context.Marks
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Mark>> GetMarksByDisciplineAndGroupAsync(int disciplineId, int groupId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entities = await context.Marks
                .Where(m => m.Work.DisciplineId == disciplineId &&
                           m.Student.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }



        public async Task<bool> UpdateMarkAsync(Mark entity)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            context.Update(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMarkByStudentAndWorkAsync(int studentId, int workId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.Marks
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.WorkId == workId);
            context.Marks.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllAsync()
        {
            using var context = await contextFactory.CreateDbContextAsync();
            await context.Marks.ExecuteDeleteAsync();
        }

        public async Task<Mark?> GetMarkByStudentAndWorkAsync(int studentId, int workId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            return await context.Marks
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.WorkId == workId);
        }

        public async Task<double> GetAverageMarkByStudentAndDisciplineAsync(int studentId, int disciplineId)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var marks = await context.Marks
                .Where(m => m.StudentId == studentId && m.Work.DisciplineId == disciplineId)
                .Select(m => m.Value)
                .ToListAsync();

            var validMarks = marks
                .Where(m => double.TryParse(m, out _))
                .Select(double.Parse)
                .ToList();

            return validMarks.Count != 0 ? validMarks.Average() : 0;
        }

        public async Task<IEnumerable<Mark>> GetMarksByDisciplinesAndGroupsAsync(List<int> disciplineIds, List<int> groupIds)
        {
            using var context = await contextFactory.CreateDbContextAsync();
            var entities = await context.Marks
                .Include(m => m.Work)
                .Where(m => disciplineIds.Contains(m.Work.DisciplineId) && groupIds.Contains(m.Student.GroupId))
                .ToListAsync();
            return entities;
        }
    }

}
