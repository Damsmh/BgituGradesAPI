using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Repositories;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BgituGrades.Services
{
    public interface IMigrationService
    {
        Task DeleteAll(CancellationToken cancellationToken);
        public Task ArchiveCurrentSemesterAsync(CancellationToken cancellationToken);
        public static int GetCurrentSemester(DateOnly date) =>
                date.Month >= 9 ? 1 : 2;
        public static int GetCurrentSemester() =>
                GetCurrentSemester(DateOnly.FromDateTime(DateTime.Now));
    }
    public class MigrationsService(IClassRepository classRepository, IDisciplineRepository disciplineRepository,
        IGroupRepository groupRepository, IMarkRepository markRepository, IStudentRepository studentRepository,
        IPresenceRepository presenceRepository, ITransferRepository transferRepository, 
        IWorkRepository workRepository, IServiceScopeFactory scopeFactory) : IMigrationService
    {
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly ITransferRepository _transferRepository = transferRepository;
        private readonly IWorkRepository _workRepository = workRepository;
        private readonly IMarkRepository _markRepository = markRepository;
        private readonly IStudentRepository _studentRepository = studentRepository;
        public async Task DeleteAll(CancellationToken cancellationToken)
        {
            await _markRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _classRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _disciplineRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _groupRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _presenceRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _transferRepository.DeleteAllAsync(cancellationToken: cancellationToken);
            await _workRepository.DeleteAllAsync(cancellationToken: cancellationToken);
        }

        public async Task ArchiveCurrentSemesterAsync(CancellationToken cancellationToken = default)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateOnly.FromDateTime(DateTime.Now);
            var semester = IMigrationService.GetCurrentSemester(now);
            var year = now.Year;

            var alreadyArchived = await db.ReportSnapshots
                .AnyAsync(a => a.Semester == semester && a.Year == year, cancellationToken);

            if (alreadyArchived)
                throw new InvalidOperationException(
                    $"Архив за {semester} семестр {year} года уже существует.");

            var students = await db.Students
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var groups = await db.Groups
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var disciplines = await db.Disciplines
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var presences = await db.Presences
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var marks = await db.Marks
                .AsNoTracking()
                .Where(m => !string.IsNullOrEmpty(m.Value))
                .Select(m => new
                {
                    m.StudentId,
                    m.Work!.DisciplineId,
                    m.Value
                })
                .ToListAsync(cancellationToken);

            var studentDict = students.ToDictionary(s => s.Id);
            var groupDict = groups.ToDictionary(g => g.Id);
            var disciplineDict = disciplines.ToDictionary(d => d.Id);

            var presenceDict = presences
                .GroupBy(p => (p.StudentId, p.DisciplineId))
                .ToDictionary(
                    g => g.Key,
                    g => $"{g.Count(p => p.IsPresent == PresenceType.PRESENT)}/{g.Count()}"
                );

            var markDict = marks
                .Select(m => new
                {
                    m.StudentId,
                    m.DisciplineId,
                    ParsedValue = double.TryParse(
                        m.Value!.Replace(',', '.'),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var val) ? val : (double?)null
                })
                .Where(m => m.ParsedValue.HasValue)
                .GroupBy(m => (m.StudentId, m.DisciplineId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(m => m.ParsedValue!.Value).ToString("0.0", CultureInfo.InvariantCulture)
                );

            var pairs = presenceDict.Keys
                .Union(markDict.Keys)
                .Distinct();

            var archives = pairs.Select(pair =>
            {
                studentDict.TryGetValue(pair.StudentId, out var student);
                groupDict.TryGetValue(student?.GroupId ?? 0, out var group);
                disciplineDict.TryGetValue(pair.DisciplineId, out var discipline);

                return new ReportSnapshot
                {
                    Semester = semester,
                    Year = year,
                    StudentId = pair.StudentId,
                    OfficialStudentId = student?.OfficialId ?? 0,
                    StudentName = student?.Name ?? string.Empty,
                    GroupId = student?.GroupId ?? 0,
                    GroupName = group?.Name ?? string.Empty,
                    DisciplineId = pair.DisciplineId,
                    DisciplineName = discipline?.Name ?? string.Empty,
                    Presences = presenceDict.TryGetValue(pair, out var p) ? p : "0/0",
                    Marks = markDict.TryGetValue(pair, out var m) ? m : "0.0"
                };
            }).ToList();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await db.BulkInsertAsync(archives, cancellationToken: cancellationToken);

                await db.Presences.ExecuteDeleteAsync(cancellationToken);
                await db.Marks.ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
