using BgituGrades.Entities;
using BgituGrades.Hubs;
using BgituGrades.Models.Report;
using BgituGrades.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using OfficeOpenXml;

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

            var headColor = System.Drawing.Color.FromArgb(102, 0, 153);
            var zebraColor = System.Drawing.Color.FromArgb(245, 245, 245);

            var sortedGroups = groups.OrderBy(g => g.Name).ToList();
            var allowedDisciplineIds = disciplines.Select(d => d.Id).ToHashSet();

            var disciplinesByGroup = groups.ToDictionary(
                g => g.Id,
                g => g.Classes?.Where(c => c.Discipline != null && allowedDisciplineIds.Contains(c.Discipline.Id))
                               .Select(c => c.Discipline)
                               .DistinctBy(d => d.Id)
                               .OrderBy(d => d.Name).ToList() ?? new List<Discipline>()
            );

            var cellGroups = worksheet.Cells[1, 1];
            cellGroups.Value = "Группы";
            cellGroups.Style.Font.Bold = true;
            cellGroups.Style.Font.Color.SetColor(System.Drawing.Color.White);
            cellGroups.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cellGroups.Style.Fill.BackgroundColor.SetColor(headColor);
            cellGroups.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            int maxCols = disciplinesByGroup.Any() ? disciplinesByGroup.Max(g => g.Value.Count) : 1;
            var disciplinesHeaderRange = worksheet.Cells[1, 2, 1, maxCols + 1];
            disciplinesHeaderRange.Merge = true;
            disciplinesHeaderRange.Value = "Дисциплины";
            disciplinesHeaderRange.Style.Font.Bold = true;
            disciplinesHeaderRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
            disciplinesHeaderRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            disciplinesHeaderRange.Style.Fill.BackgroundColor.SetColor(headColor);
            disciplinesHeaderRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            var allMarks = await _markRepository.GetMarksByDisciplinesAndGroupsAsync(disciplines.Select(d => d.Id).ToList(), sortedGroups.Select(g => g.Id).ToList());
            var markDict = allMarks
                .Where(m => m.Work != null && !string.IsNullOrEmpty(m.Value))
                .Select(m => new {
                    m.StudentId,
                    m.Work.DisciplineId,
                    ParsedValue = double.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : (double?)null
                })
                .Where(m => m.ParsedValue.HasValue)
                .GroupBy(m => new { m.StudentId, m.DisciplineId })
                .ToDictionary(g => (g.Key.StudentId, g.Key.DisciplineId), g => g.Average(m => m.ParsedValue.Value));

            int currentRow = 2;
            foreach (var group in sortedGroups)
            {
                var groupRowRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                worksheet.Cells[currentRow, 1].Value = group.Name;
                groupRowRange.Style.Font.Bold = true;
                groupRowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupRowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                var groupDisciplines = disciplinesByGroup[group.Id];
                for (int i = 0; i < groupDisciplines.Count; i++)
                {
                    var cell = worksheet.Cells[currentRow, i + 2];
                    cell.Value = groupDisciplines[i].Name;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                currentRow++;

                var groupStudents = students.Where(s => s.GroupId == group.Id).OrderBy(s => s.Name).ToList();
                for (int sIdx = 0; sIdx < groupStudents.Count; sIdx++)
                {
                    var student = groupStudents[sIdx];
                    worksheet.Cells[currentRow, 1].Value = student.Name;

                    if (sIdx % 2 != 0)
                    {
                        var rowRange = worksheet.Cells[currentRow, 1, currentRow, groupDisciplines.Count + 1];
                        rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(zebraColor);
                    }

                    for (int i = 0; i < groupDisciplines.Count; i++)
                    {
                        var cell = worksheet.Cells[currentRow, i + 2];
                        if (markDict.TryGetValue((student.Id, groupDisciplines[i].Id), out var avgMark))
                        {
                            cell.Value = avgMark;
                            cell.Style.Numberformat.Format = "0.0";
                        }
                        else cell.Value = 0;
                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    currentRow++;
                }
            }

            worksheet.Cells[1, 1, currentRow - 1, maxCols + 1].AutoFitColumns();
            var fullRange = worksheet.Cells[1, 1, currentRow - 1, maxCols + 1];
            fullRange.Style.Border.Top.Style = fullRange.Style.Border.Bottom.Style =
            fullRange.Style.Border.Left.Style = fullRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            fullRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            worksheet.View.FreezePanes(2, 2);

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return stream.ToArray();
        }

        private static async Task<byte[]> GeneratePresenceExcelAsync(IPresenceRepository _presenceRepository, IEnumerable<Group> groups, IEnumerable<Discipline> disciplines, IEnumerable<Student> students)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт посещаемости");

            var headColor = System.Drawing.Color.FromArgb(102, 0, 153);
            var zebraColor = System.Drawing.Color.FromArgb(245, 245, 245);

            var sortedGroups = groups.OrderBy(g => g.Name).ToList();
            var allowedDisciplineIds = disciplines.Select(d => d.Id).ToHashSet();

            var disciplinesByGroup = groups.ToDictionary(
                g => g.Id,
                g => g.Classes?.Where(c => c.Discipline != null && allowedDisciplineIds.Contains(c.Discipline.Id))
                               .Select(c => c.Discipline)
                               .DistinctBy(d => d.Id)
                               .OrderBy(d => d.Name).ToList() ?? new List<Discipline>()
            );

            worksheet.Cells[1, 1].Value = "Группы";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(headColor);
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            int maxCols = disciplinesByGroup.Any() ? disciplinesByGroup.Max(g => g.Value.Count) : 1;
            var headRange = worksheet.Cells[1, 2, 1, maxCols + 1];
            headRange.Merge = true;
            headRange.Value = "Дисциплины";
            headRange.Style.Font.Bold = true;
            headRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
            headRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headRange.Style.Fill.BackgroundColor.SetColor(headColor);
            headRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            var allPresences = await _presenceRepository.GetPresencesByDisciplinesAndGroupsAsync(disciplines.Select(d => d.Id).ToList(), sortedGroups.Select(g => g.Id).ToList());
            var presenceDict = allPresences
                .GroupBy(m => new { m.StudentId, m.DisciplineId })
                .ToDictionary(
                    g => (g.Key.StudentId, g.Key.DisciplineId),
                    g => (Present: g.Count(m => m.IsPresent == PresenceType.PRESENT), Total: g.Count())
                );

            int currentRow = 2;
            foreach (var group in sortedGroups)
            {
                var groupRowRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                worksheet.Cells[currentRow, 1].Value = group.Name;
                groupRowRange.Style.Font.Bold = true;
                groupRowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupRowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                var groupDisciplines = disciplinesByGroup[group.Id];
                for (int i = 0; i < groupDisciplines.Count; i++)
                {
                    var cell = worksheet.Cells[currentRow, i + 2];
                    cell.Value = groupDisciplines[i].Name;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                currentRow++;

                var groupStudents = students.Where(s => s.GroupId == group.Id).OrderBy(s => s.Name).ToList();
                for (int sIdx = 0; sIdx < groupStudents.Count; sIdx++)
                {
                    var student = groupStudents[sIdx];
                    worksheet.Cells[currentRow, 1].Value = student.Name;

                    if (sIdx % 2 != 0)
                    {
                        var rowRange = worksheet.Cells[currentRow, 1, currentRow, groupDisciplines.Count + 1];
                        rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(zebraColor);
                    }

                    for (int i = 0; i < groupDisciplines.Count; i++)
                    {
                        var cell = worksheet.Cells[currentRow, i + 2];
                        if (presenceDict.TryGetValue((student.Id, groupDisciplines[i].Id), out var stats))
                            cell.Value = $"{stats.Present}/{stats.Total}";
                        else cell.Value = "0/0";
                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    currentRow++;
                }
            }

            worksheet.Cells[1, 1, currentRow - 1, maxCols + 1].AutoFitColumns();
            var borderRange = worksheet.Cells[1, 1, currentRow - 1, maxCols + 1];
            borderRange.Style.Border.Top.Style = borderRange.Style.Border.Bottom.Style =
            borderRange.Style.Border.Left.Style = borderRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            borderRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            worksheet.View.FreezePanes(2, 2);

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return stream.ToArray();
        }
    }
}
