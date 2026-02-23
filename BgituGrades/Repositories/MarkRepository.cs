using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

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
    }

    public class MarkRepository(AppDbContext dbContext) : IMarkRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Mark> CreateMarkAsync(Mark entity)
        {
            await _dbContext.Marks.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<Mark>> GetAllMarksAsync()
        {
            var entities = await _dbContext.Marks
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Mark>> GetMarksByDisciplineAndGroupAsync(int disciplineId, int groupId)
        {
            var entities = await _dbContext.Marks
                .Where(m => m.Work.DisciplineId == disciplineId &&
                           m.Student.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }



        public async Task<bool> UpdateMarkAsync(Mark entity)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMarkByStudentAndWorkAsync(int studentId, int workId)
        {
            var entity = await _dbContext.Marks
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.WorkId == workId);
            _dbContext.Marks.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task DeleteAllAsync()
        {
            await _dbContext.Marks.ExecuteDeleteAsync();
        }

        public async Task<Mark?> GetMarkByStudentAndWorkAsync(int studentId, int workId)
        {
            return await _dbContext.Marks
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.WorkId == workId);
        }
    }

}
