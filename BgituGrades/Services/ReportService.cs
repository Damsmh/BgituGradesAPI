using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Hubs;
using BgituGrades.Models.Report;
using BgituGrades.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using OfficeOpenXml;
using System.Globalization;

namespace BgituGrades.Services
{
    public interface IReportService
    {
        Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId, CancellationToken cancellationToken);
    }

    public class ReportService(
        IHubContext<ReportHub> hubContext,
        IDistributedCache cache,
        IServiceScopeFactory scopeFactory) : IReportService
    {
        protected readonly IHubContext<ReportHub> _hubContext = hubContext;
        protected readonly IDistributedCache _cache = cache;
        protected readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        public async Task<Guid> GenerateReportAsync(ReportRequest request, string connectionId, CancellationToken cancellationToken)
        {
            var reportId = Guid.NewGuid();
            await _hubContext.Groups.AddToGroupAsync(connectionId, reportId.ToString());

            _ = Task.Run(async () => await GenerateWithProgress(reportId, request, cancellationToken: cancellationToken), cancellationToken: cancellationToken);

            return reportId;
        }

        protected virtual async Task GenerateWithProgress(Guid reportId, ReportRequest request, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var groupRepo = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
            var disciplineRepo = scope.ServiceProvider.GetRequiredService<IDisciplineRepository>();
            var studentRepo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
            var markRepo = scope.ServiceProvider.GetRequiredService<IMarkRepository>();
            var presenceRepo = scope.ServiceProvider.GetRequiredService<IPresenceRepository>();
            var classService = scope.ServiceProvider.GetRequiredService<IClassService>();

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            try
            {
                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 10, "Загрузка данных...");
                IEnumerable<Group> groups;
                if (request.GroupIds != null)
                {
                    groups = await groupRepo.GetGroupsByIdsAsync(request.GroupIds, cancellationToken: cancellationToken);
                }
                else
                {
                    groups = await groupRepo.GetAllAsync(cancellationToken: cancellationToken);
                }

                IEnumerable<Discipline> disciplines;
                if (request.DisciplineIds != null)
                {
                    disciplines = await disciplineRepo.GetDisciplinesByIdsAsync(request.DisciplineIds, cancellationToken: cancellationToken);
                }
                else
                {
                    disciplines = await disciplineRepo.GetByGroupIdsAsync([.. groups.Select(g => g.Id)], cancellationToken: cancellationToken);
                }

                IEnumerable<Student> students;
                if (request.StudentIds != null)
                {
                    students = await studentRepo.GetStudentsByIdsAsync(request.StudentIds, cancellationToken: cancellationToken);
                }
                else
                {
                    students = await studentRepo.GetStudentsByGroupIdsAsync([.. groups.Select(g => g.Id)], cancellationToken: cancellationToken);
                }

                if (!groups.Any() || !disciplines.Any())
                {
                    throw new Exception("Нет данных для формирования отчета");
                }

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 40, "Генерация Excel файла...");

                TablePreview result;
                if (request.ReportType == ReportType.MARK)
                {
                    result = await GenerateMarksExcelAsync(markRepo, groups, disciplines, students, cancellationToken);
                }
                else
                {
                    result = await GeneratePresenceExcelAsync(presenceRepo, groups, disciplines, students, classService, cancellationToken);
                }

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportProgress", reportId.ToString(), 80, "Сохранение...");

                await _cache.SetAsync($"report_{reportId}", result.ExcelBytes!, cacheOptions);

                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportReady", reportId.ToString(), $"https://maxim.pamagiti.site/api/report/{reportId}/download", result.Preview);
            }
            catch (Exception ex)
            {
                await _hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("Error", ex.Message, ex.StackTrace);
            }
        }

        protected static async Task<TablePreview> GenerateMarksExcelAsync(IMarkRepository _markRepository, IEnumerable<Group> groups,
            IEnumerable<Discipline> disciplines, IEnumerable<Student> students, CancellationToken cancellationToken)
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
                               .DistinctBy(d => d!.Id)
                               .OrderBy(d => d!.Name).ToList() ?? []
            );


            int maxCols = disciplinesByGroup.Count != 0 ? disciplinesByGroup.Max(g => g.Value.Count) : 1;

            var allMarks = await _markRepository.GetMarksByDisciplinesAndGroupsAsync(disciplines.Select(d => d.Id).ToList(), sortedGroups.Select(g => g.Id).ToList(), cancellationToken: cancellationToken);
            var markDict = allMarks
                .Where(m => m.Work != null && !string.IsNullOrEmpty(m.Value))
                .Select(m => new
                {
                    m.StudentId,
                    m.Work!.DisciplineId,
                    ParsedValue = double.TryParse(m.Value!.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : (double?)null
                })
                .Where(m => m.ParsedValue.HasValue)
                .GroupBy(m => new { m.StudentId, m.DisciplineId })
                .ToDictionary(g => (g.Key.StudentId, g.Key.DisciplineId), g => g.Average(m => m.ParsedValue!.Value));

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
                    cell.Value = groupDisciplines[i]!.Name;
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
                        if (markDict.TryGetValue((student.Id, groupDisciplines[i]!.Id), out var avgMark))
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

            var preview = new ReportPreviewDto();

            foreach (var group in sortedGroups)
            {
                var groupDisciplines = disciplinesByGroup[group.Id];
                var cells = new List<string> { group.Name! };
                cells.AddRange(groupDisciplines.Select(d => d!.Name)!);
                preview.Rows.Add(new PreviewRow
                {
                    IsGroupHeader = true,
                    Cells = cells
                });


                var groupStudents = students
                    .Where(s => s.GroupId == group.Id)
                    .OrderBy(s => s.Name);

                foreach (var student in groupStudents)
                {
                    var scells = new List<string> { student.Name! };
                    scells.AddRange(groupDisciplines.Select(d =>
                        markDict.TryGetValue((student.Id, d!.Id), out var mark)
                            ? mark.ToString("0.0", CultureInfo.InvariantCulture)
                            : "0"
                    ));

                    preview.Rows.Add(new PreviewRow
                    {
                        IsGroupHeader = false,
                        Cells = scells
                    });
                }
            }

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return new TablePreview
            {
                ExcelBytes = stream.ToArray(),
                Preview = preview
            };
        }

        protected static async Task<TablePreview> GeneratePresenceExcelAsync(IPresenceRepository _presenceRepository, IEnumerable<Group> groups,
            IEnumerable<Discipline> disciplines, IEnumerable<Student> students, IClassService _classService, CancellationToken cancellationToken)
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
                               .DistinctBy(d => d!.Id)
                               .OrderBy(d => d!.Name).ToList() ?? []
            );


            int maxCols = disciplinesByGroup.Count != 0 ? disciplinesByGroup.Max(g => g.Value.Count) : 1;

            var allPresences = await _presenceRepository.GetPresencesByDisciplinesAndGroupsAsync([.. disciplines.Select(d => d.Id)], [.. sortedGroups.Select(g => g.Id)], cancellationToken: cancellationToken);
            var groupDisciplinePairs = sortedGroups
                .SelectMany(g => disciplinesByGroup[g.Id]
                    .Select(d => (GroupId: g.Id, DisciplineId: d!.Id)))
                .Distinct()
                .ToList();

            var scheduleTasks = groupDisciplinePairs.Select(async pair =>
            {
                var dates = await _classService.GenerateScheduleDatesAsync(pair.GroupId, pair.DisciplineId, cancellationToken);
                return (pair.GroupId, pair.DisciplineId, Total: dates.Count());
            });
            var scheduleResults = await Task.WhenAll(scheduleTasks);


            var scheduleTotalDict = scheduleResults
                .ToDictionary(
                    r => (r.GroupId, r.DisciplineId),
                    r => r.Total
                );
            var studentGroupDict = students.ToDictionary(s => s.Id, s => s.GroupId);
            var presenceDict = allPresences
                .GroupBy(m => new { m.StudentId, m.DisciplineId })
                .ToDictionary(
                    g => (g.Key.StudentId, g.Key.DisciplineId),
                    g => (
                        Absent: g.Count(m => m.IsPresent != PresenceType.PRESENT),
                        Total: studentGroupDict.TryGetValue(g.Key.StudentId, out var gId)
                            && scheduleTotalDict.TryGetValue((gId, g.Key.DisciplineId), out var t) ? t : 0
                    )
                );

            int currentRow = 1;
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
                    cell.Value = groupDisciplines[i]!.Name;
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
                        var disciplineId = groupDisciplines[i]!.Id;
                        var total = scheduleTotalDict.TryGetValue((group.Id, disciplineId), out var t) ? t : 0;
                        var absent = presenceDict.TryGetValue((student.Id, disciplineId), out var stats) ? stats.Absent : 0;
                        var present = total - absent;

                        cell.Value = $"{present}/{total}";
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

            var preview = new ReportPreviewDto();

            foreach (var group in sortedGroups)
            {
                var groupDisciplines = disciplinesByGroup[group.Id];
                var cells = new List<string> { group.Name! };
                cells.AddRange(groupDisciplines.Select(d => d!.Name)!);
                preview.Rows.Add(new PreviewRow
                {
                    IsGroupHeader = true,
                    Cells = cells
                });


                var groupStudents = students
                    .Where(s => s.GroupId == group.Id)
                    .OrderBy(s => s.Name);

                foreach (var student in groupStudents)
                {
                    var scells = new List<string> { student.Name! };
                    scells.AddRange(groupDisciplines.Select(d =>
                    {
                        var total = scheduleTotalDict.TryGetValue((group.Id, d!.Id), out var t) ? t : 0;
                        var absent = presenceDict.TryGetValue((student.Id, d!.Id), out var stats) ? stats.Absent : 0;
                        return $"{total - absent}/{total}";
                    }));

                    preview.Rows.Add(new PreviewRow
                    {
                        IsGroupHeader = false,
                        Cells = scells
                    });
                }
            }

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream, cancellationToken);
            return new TablePreview
            {
                ExcelBytes = stream.ToArray(),
                Preview = preview
            };
        }
    }
}
