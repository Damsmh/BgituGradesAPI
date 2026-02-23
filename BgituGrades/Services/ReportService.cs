using BgituGrades.Entities;
using BgituGrades.Hubs;
using BgituGrades.Models.Report;
using BgituGrades.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;

namespace BgituGrades.Services
{
    public interface IReportService
    {
        Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId);
    }

    public class ReportService(
        IHubContext<ReportHub> hubContext,
        IMarkRepository markRepository,
        IPresenceRepository presenceRepository,
        IGroupRepository groupRepository,
        IDisciplineRepository disciplineRepository,
        IStudentRepository studentRepository,
        IMemoryCache cache) : IReportService
    {
        private readonly IHubContext<ReportHub> _hubContext = hubContext;
        private readonly IMarkRepository _markRepository = markRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IMemoryCache _cache = cache;

        public async Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId)
        {
            var reportId = Guid.NewGuid();
            await _hubContext.Groups.AddToGroupAsync(connectionId, reportId.ToString());
            await Task.Run(async () => await GenerateWithProgress(reportId, request, connectionId));
            return reportId;
        }

        private async Task GenerateWithProgress(Guid reportId, ReportRequest request, string connectionId)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 10, "Загружаем группы...");

            var groupsTask = LoadGroupsAsync(request.GroupIds);
            var disciplinesTask = LoadDisciplinesAsync(request.DisciplineIds);
            var studentsTask = LoadStudentsAsync(request.StudentIds);

            await Task.WhenAll(groupsTask, disciplinesTask, studentsTask);

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 40, "Формируем Excel...");

            var excelBytes = await GenerateExcelAsync(await groupsTask, await disciplinesTask, await studentsTask, request);

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 90, "Сохраняем файл...");

            _cache.Set($"report_{reportId}", excelBytes, cacheEntryOptions);

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportReady", reportId.ToString(), $"/api/report/{reportId}/download");
        }

        private async Task<IEnumerable<Group>> LoadGroupsAsync(int[] groupIds) =>
            await _groupRepository.GetGroupsByIdsAsync(groupIds);

        private async Task<IEnumerable<Discipline>> LoadDisciplinesAsync(int[] disciplineIds) =>
            await _disciplineRepository.GetDisciplinesByIdsAsync(disciplineIds);

        private async Task<IEnumerable<Student>> LoadStudentsAsync(int[] studentIds) =>
            await _studentRepository.GetStudentsByIdsAsync(studentIds);

        private async Task<byte[]> GenerateExcelAsync(IEnumerable<Group> groups, IEnumerable<Discipline> disciplines, IEnumerable<Student> students, ReportRequest request)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчет");

            Dictionary<(int StudentId, int DisciplineId), double> markDict = new();

            try
            {
                if (disciplines.Any() && groups.Any())
                {
                    var allMarks = await _markRepository.GetMarksByDisciplineAndGroupAsync(disciplines.First().Id, groups.First().Id);
                    markDict = allMarks
                        .Where(m => double.TryParse(m.Value, out _))
                        .GroupBy(m => new { m.StudentId, m.Work.DisciplineId })
                        .ToDictionary(
                            g => (g.Key.StudentId, g.Key.DisciplineId),
                            g => g.Average(m => double.Parse(m.Value))
                        );
                }
            }
            catch
            {
                
            }

            worksheet.Cells[1, 1].Value = "Группа&Студенты";
            var disciplinesList = disciplines.OrderBy(d => d.Name).ToList();
            for (int i = 0; i < disciplinesList.Count; i++)
            {
                worksheet.Cells[1, i + 2].Value = disciplinesList[i].Name;
            }

            int row = 2;
            foreach (var group in groups.OrderBy(g => g.Name))
            {
                worksheet.Cells[row, 1].Value = group.Name;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var groupStudents = students.Where(s => s.GroupId == group.Id);
                foreach (var student in groupStudents.OrderBy(s => s.Name))
                {
                    worksheet.Cells[row, 1].Value = $"{student.Name}";

                    for (int i = 0; i < disciplinesList.Count; i++)
                    {
                        var discipline = disciplinesList[i];
                        var key = (student.Id, discipline.Id);
                        if (markDict.TryGetValue(key, out var avgMark) && avgMark > 0)
                        {
                            worksheet.Cells[row, i + 2].Value = avgMark;
                            worksheet.Cells[row, i + 2].Style.Numberformat.Format = "0.0";
                        }
                    }
                    row++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return stream.ToArray();
        }
    }
}
