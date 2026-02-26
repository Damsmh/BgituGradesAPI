using BgituGrades.Entities;
using BgituGrades.Hubs;
using BgituGrades.Models.Report;
using BgituGrades.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using OfficeOpenXml;
using System.Diagnostics.Eventing.Reader;

namespace BgituGrades.Services
{
    public interface IReportService
    {
        Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId);
    }

    public class ReportService(
        IHubContext<ReportHub> hubContext,
        IDistributedCache cache,
        IServiceScopeFactory scopeFactory) : IReportService
    {
        private readonly IHubContext<ReportHub> _hubContext = hubContext;
        private readonly IDistributedCache _cache = cache;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        public async Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId)
        {
            var reportId = Guid.NewGuid();
            await _hubContext.Groups.AddToGroupAsync(connectionId, reportId.ToString());

            _ = Task.Run(async () => await GenerateWithProgress(reportId, request));

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

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            try
            {
                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 10, "Загрузка данных...");
                IEnumerable<Group> groups;
                if (request.GroupIds != null)
                {
                    groups = await groupRepo.GetGroupsByIdsAsync(request.GroupIds);
                } else {
                    groups = await groupRepo.GetAllAsync();
                }
                 
                IEnumerable<Discipline> disciplines;
                if (request.DisciplineIds !=  null)
                {
                    disciplines = await disciplineRepo.GetDisciplinesByIdsAsync(request.DisciplineIds);
                } else {
                    disciplines = await disciplineRepo.GetByGroupIdsAsync(groups.Select(g => g.Id).ToArray());
                }

                IEnumerable<Student> students;
                if (request.StudentIds != null) {
                    students = await studentRepo.GetStudentsByIdsAsync(request.StudentIds);
                } else {
                    students = await studentRepo.GetStudentsByGroupIdsAsync(groups.Select(g => g.Id).ToArray());
                }

                if (!groups.Any() || !disciplines.Any())
                {
                    throw new Exception("Нет данных для формирования отчета");
                }

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 40, "Генерация Excel файла...");

                byte[] excelBytes;
                if (request.ReportType == ReportType.MARK)
                {
                    excelBytes = await GenerateMarksExcelAsync(markRepo, groups, disciplines, students);
                }
                else
                {
                    excelBytes = await GeneratePresenceExcelAsync(presenceRepo, groups, disciplines, students);
                }

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 80, "Сохранение...");

                await _cache.SetAsync($"report_{reportId}", excelBytes, cacheOptions);

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportReady", reportId.ToString(), $"https://maxim.pamagiti.site/api/report/{reportId}/download");
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("Error", ex.Message);
            }
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
                        .Where(m => m.Work != null && !string.IsNullOrEmpty(m.Value)) 
                        .Select(m => new
                        {
                            m.StudentId,
                            m.Work.DisciplineId,
                            ParsedValue = double.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : (double?)null
                        })
                        .Where(m => m.ParsedValue.HasValue)
                        .GroupBy(m => new { m.StudentId, m.DisciplineId })
                        .ToDictionary(
                            g => (g.Key.StudentId, g.Key.DisciplineId),
                            g => g.Average(m => m.ParsedValue.Value)
                        );
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при формировании данных успеваемости: {ex.Message}", ex);
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

                        if (markDict.TryGetValue(key, out var avgMark))
                        {
                            worksheet.Cells[row, i + 2].Value = avgMark;
                            worksheet.Cells[row, i + 2].Style.Numberformat.Format = "0.0";
                            worksheet.Cells[row, i + 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }
                        else
                        {
                            worksheet.Cells[row, i + 2].Value = "0.0";
                            worksheet.Cells[row, i + 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
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
