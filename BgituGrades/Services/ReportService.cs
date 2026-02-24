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
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory) : IReportService
    {
        private readonly IHubContext<ReportHub> _hubContext = hubContext;
        private readonly IMemoryCache _cache = cache;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        public async Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId)
        {
            var reportId = Guid.NewGuid();
            await _hubContext.Groups.AddToGroupAsync(connectionId, reportId.ToString());
            _ =  Task.Run(async () => await GenerateWithProgress(reportId, request));
            return reportId;
        }

        private async Task GenerateWithProgress(Guid reportId, ReportRequest request)
        {
            using var scope = _scopeFactory.CreateScope();

            var groupRepo = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
            var disciplineRepo = scope.ServiceProvider.GetRequiredService<IDisciplineRepository>();
            var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            var markRepo = scope.ServiceProvider.GetRequiredService<IMarkRepository>();
            var presenceRepo = scope.ServiceProvider.GetRequiredService<IPresenceRepository>();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 10, "Загружаем группы...");

            var groups = await groupRepo.GetGroupsByIdsAsync(request.GroupIds);
            var disciplines = await disciplineRepo.GetDisciplinesByIdsAsync(request.DisciplineIds);
            var students = await studentRepo.GetStudentsByIdsAsync(request.StudentIds);

            if (!groups.Any() || !disciplines.Any())
            {
                throw new Exception("Нет данных для формирования отчета (проверьте ID групп и дисциплин)");
            }

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 40, "Формируем Excel...");

            byte[] excelBytes;
            try
            {
                if (request.ReportType == ReportType.MARK)
                {
                    excelBytes = await GenerateMarksExcelAsync(markRepo, groups, disciplines, students);
                }
                else if (request.ReportType == ReportType.PRESENCE)
                {
                    excelBytes = await GeneratePresenceExcelAsync(presenceRepo, groups, disciplines, students);
                }
                else
                {
                    throw new Exception("Invalid report type");
                }
            } catch (Exception ex)
            {
                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("Error", ex.Message);
                return;
            }
            

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), 90, "Сохраняем файл...");

            _cache.Set($"report_{reportId}", excelBytes, cacheEntryOptions);

            await _hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportReady", reportId.ToString(), $"https://maxim.pamagiti.site/api/report/{reportId}/download");
        }

        private static async Task<byte[]> GenerateMarksExcelAsync(IMarkRepository _markRepository, IEnumerable<Group> groups, IEnumerable<Discipline> disciplines, IEnumerable<Student> students)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт успеваемости");

            var disciplineIds = disciplines.Select(d => d.Id).ToList();
            var groupIds = groups.Select(g => g.Id).ToList();

            Dictionary<(int StudentId, int DisciplineId), double> markDict = new();

            try
            {
                if (disciplines.Any() && groups.Any())
                {
                    var allMarks = await _markRepository.GetMarksByDisciplinesAndGroupsAsync(disciplineIds, groupIds);
                    markDict = allMarks
                        .Where(m => double.TryParse(m.Value, out _))
                        .GroupBy(m => new { m.StudentId, m.Work.DisciplineId })
                        .ToDictionary(
                            g => (g.Key.StudentId, g.Key.DisciplineId),
                            g => g.Average(m => double.Parse(m.Value))
                        );
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при формировании данных успеваемости: {ex.Message}", ex);
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

        private static async Task<byte[]> GeneratePresenceExcelAsync(IPresenceRepository _presenceRepository, IEnumerable<Group> groups, IEnumerable<Discipline> disciplines, IEnumerable<Student> students)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт посещаемости");

            var disciplineIds = disciplines.Select(d => d.Id).ToList();
            var groupIds = groups.Select(g => g.Id).ToList();


            Dictionary<(int StudentId, int DisciplineId), (int Present, int Total)> presenceDict = new();

            try
            {
                if (disciplines.Any() && groups.Any())
                {
                    var allPresences = await _presenceRepository.GetPresencesByDisciplinesAndGroupsAsync(disciplineIds, groupIds);

                    presenceDict = allPresences
                        .GroupBy(m => new { m.StudentId, m.DisciplineId })
                        .ToDictionary(
                            g => (g.Key.StudentId, g.Key.DisciplineId),
                            g => (
                                Present: g.Count(m => m.IsPresent == PresenceType.PRESENT),
                                Total: g.Count()
                            )
                        );
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при формировании данных посещаемости: {ex.Message}", ex);
            }

            worksheet.Cells[1, 1].Value = "Группа & Студенты";
            var disciplinesList = disciplines.OrderBy(d => d.Name).ToList();
            for (int i = 0; i < disciplinesList.Count; i++)
            {
                worksheet.Cells[1, i + 2].Value = disciplinesList[i].Name;
                worksheet.Cells[1, i + 2].Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var group in groups.OrderBy(g => g.Name))
            {
                worksheet.Cells[row, 1].Value = group.Name;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, disciplinesList.Count + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, disciplinesList.Count + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                row++;

                var groupStudents = students.Where(s => s.GroupId == group.Id).OrderBy(s => s.Name);
                foreach (var student in groupStudents)
                {
                    worksheet.Cells[row, 1].Value = student.Name;

                    for (int i = 0; i < disciplinesList.Count; i++)
                    {
                        var discipline = disciplinesList[i];
                        var key = (student.Id, discipline.Id);

                        if (presenceDict.TryGetValue(key, out var stats))
                        {
                            worksheet.Cells[row, i + 2].Value = $"{stats.Present}/{stats.Total}";

                            worksheet.Cells[row, i + 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }
                        else
                        {
                            worksheet.Cells[row, i + 2].Value = "0/0";
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
