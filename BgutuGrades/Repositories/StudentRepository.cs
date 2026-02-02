using BgutuGrades.Data;
using BgutuGrades.Models.Class;
using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;
using Grades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgutuGrades.Repositories
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllStudentsAsync();
        Task<IEnumerable<Student>> GetStudentsByGroupAsync(int groupId);
        Task<IEnumerable<FullGradeMarkResponse>> GetMarksGrade(IEnumerable<Work> works, int groupId, int disciplineId);
        Task<IEnumerable<FullGradePresenceResponse>> GetPresenseGrade(IEnumerable<ClassDateResponse> scheduleDates, int groupId, int disciplineId);
        Task<Student> CreateStudentAsync(Student entity);
        Task<Student?> GetByIdAsync(int id);
        Task<bool> UpdateStudentAsync(Student entity);
        Task<bool> DeleteStudentAsync(int id);
    }

    public class StudentRepository(AppDbContext dbContext) : IStudentRepository
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<Student> CreateStudentAsync(Student entity)
        {
            await _dbContext.Students.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            _dbContext.Students.Remove(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Students.FindAsync(id);
            return entity;
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
        {
            var entities = await _dbContext.Students
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<Student>> GetStudentsByGroupAsync(int groupId)
        {
            var entities = await _dbContext.Students
                .Where(s => s.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync();
            return entities;
        }

        public async Task<IEnumerable<FullGradePresenceResponse>> GetPresenseGrade(IEnumerable<ClassDateResponse> scheduleDates, int groupId, int disciplineId)
        {
            var studentsWithPresence = await _dbContext.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    Presences = s.Presences
                        .Where(p => p.DisciplineId == disciplineId)
                        .Select(p => new { p.Date, p.IsPresent })
                })
                .AsNoTracking()
                .ToListAsync();

            var result = studentsWithPresence.Select(s => new FullGradePresenceResponse
            {
                StudentId = s.Id,
                Name = s.Name,
                Presences = scheduleDates.Select(date => new GradePresenceResponse
                {
                    ClassId = date.Id,
                    ClassType = date.ClassType,
                    Date = date.Date,
                    IsPresent = s.Presences.FirstOrDefault(p => p.Date == date.Date)?.IsPresent ?? PresenceType.PRESENT
                }).ToList()
            });

            return result;
        }

        public async Task<IEnumerable<FullGradeMarkResponse>> GetMarksGrade(IEnumerable<Work> works, int groupId, int disciplineId)
        {
            var studentsWithMarks = await _dbContext.Students
                .Where(s => s.GroupId == groupId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    Marks = s.Marks
                        .Where(m => m.Work.DisciplineId == disciplineId)
                        .Select(m => new { m.WorkId, m.Value })
                })
                .AsNoTracking()
                .ToListAsync();

                var result = studentsWithMarks.Select(s => new FullGradeMarkResponse
                {
                    StudentId = s.Id,
                    Name = s.Name,
                    Marks = works.Select(work => new GradeMarkResponse
                    {
                        WorkId = work.Id,
                        Name = work.Name,
                        Value = s.Marks.FirstOrDefault(m => m.WorkId == work.Id)?.Value ?? ""
                    }).ToList()
                });

            return result;
        }

        public async Task<bool> UpdateStudentAsync(Student entity)
        {
            _dbContext.Update(entity);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
