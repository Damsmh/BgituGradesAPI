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

            var disciplinesList = disciplines.OrderBy(d => d.Name).ToList();
            var sortedGroups = groups.OrderBy(g => g.Name).ToList();
            int maxDisciplineCount = disciplinesList.Count;

            var cellGroups = worksheet.Cells[1, 1];
            cellGroups.Value = "Группы";
            cellGroups.Style.Font.Bold = true;
            cellGroups.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            cellGroups.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cellGroups.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkViolet);
            cellGroups.Style.Font.Color.SetColor(System.Drawing.Color.Black);

            if (maxDisciplineCount > 0)
            {
                var disciplinesHeaderRange = worksheet.Cells[1, 2, 1, maxDisciplineCount + 1];
                disciplinesHeaderRange.Merge = true;
                disciplinesHeaderRange.Value = "Дисциплины";
                disciplinesHeaderRange.Style.Font.Bold = true;
                disciplinesHeaderRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                disciplinesHeaderRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                disciplinesHeaderRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkViolet);
                disciplinesHeaderRange.Style.Font.Color.SetColor(System.Drawing.Color.Black);
            }

            Dictionary<(int StudentId, int DisciplineId), double> markDict = new();
            try
            {
                if (disciplinesList.Any() && sortedGroups.Any())
                {
                    var allMarks = await _markRepository.GetMarksByDisciplinesAndGroupsAsync(disciplines.Select(d => d.Id).ToList(), sortedGroups.Select(g => g.Id).ToList());
                    markDict = allMarks
                        .Where(m => m.Work != null && !string.IsNullOrEmpty(m.Value))
                        .Select(m => new {
                            m.StudentId,
                            m.Work.DisciplineId,
                            ParsedValue = double.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : (double?)null
                        })
                        .Where(m => m.ParsedValue.HasValue)
                        .GroupBy(m => new { m.StudentId, m.DisciplineId })
                        .ToDictionary(g => (g.Key.StudentId, g.Key.DisciplineId), g => g.Average(m => m.ParsedValue.Value));
                }
            }
            catch (Exception ex) { throw new Exception($"Ошибка данных: {ex.Message}"); }

            int currentRow = 2;

            foreach (var group in sortedGroups)
            {
                var groupNameCell = worksheet.Cells[currentRow, 1];
                groupNameCell.Value = group.Name;
                groupNameCell.Style.Font.Bold = true;
                groupNameCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupNameCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                var groupDisciplineIds = group.Classes?
                    .Where(c => c.Discipline != null)
                    .Select(c => c.DisciplineId)
                    .Distinct()
                    .ToHashSet() ?? new HashSet<int>();

                for (int i = 0; i < disciplinesList.Count; i++)
                {
                    var currentDisc = disciplinesList[i];
                    var discCell = worksheet.Cells[currentRow, i + 2];

                    if (groupDisciplineIds.Contains(currentDisc.Id))
                    {
                        discCell.Value = currentDisc.Name;
                    }

                    discCell.Style.Font.Bold = true;
                    discCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    discCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                currentRow++;

                var groupStudents = students.Where(s => s.GroupId == group.Id).OrderBy(s => s.Name).ToList();
                foreach (var student in groupStudents)
                {
                    worksheet.Cells[currentRow, 1].Value = student.Name;
                    for (int i = 0; i < disciplinesList.Count; i++)
                    {
                        var disc = disciplinesList[i];
                        var cell = worksheet.Cells[currentRow, i + 2];

                        if (groupDisciplineIds.Contains(disc.Id))
                        {
                            if (markDict.TryGetValue((student.Id, disc.Id), out var avgMark))
                            {
                                cell.Value = avgMark;
                                cell.Style.Numberformat.Format = "0.0";
                            }
                            else
                            {
                                cell.Value = "0";
                            }
                        }
                        else
                        {
                            cell.Value = "-";
                        }
                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    currentRow++;
                }
            }


            worksheet.Cells.AutoFitColumns();

            var modelRange = worksheet.Cells[1, 1, currentRow - 1, maxDisciplineCount + 1];
            modelRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return stream.ToArray();
        }

        private static async Task<byte[]> GeneratePresenceExcelAsync(IPresenceRepository _presenceRepository, IEnumerable<Group> groups, IEnumerable<Discipline> disciplines, IEnumerable<Student> students)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт посещаемости");

            var disciplinesList = disciplines.OrderBy(d => d.Name).ToList();
            var sortedGroups = groups.OrderBy(g => g.Name).ToList();
            int maxDisciplineCount = disciplinesList.Count;


            var cellGroups = worksheet.Cells[1, 1];
            cellGroups.Value = "Группы";
            cellGroups.Style.Font.Bold = true;
            cellGroups.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            cellGroups.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cellGroups.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkViolet);
            cellGroups.Style.Font.Color.SetColor(System.Drawing.Color.Black);

            if (maxDisciplineCount > 0)
            {
                var disciplinesHeaderRange = worksheet.Cells[1, 2, 1, maxDisciplineCount + 1];
                disciplinesHeaderRange.Merge = true;
                disciplinesHeaderRange.Value = "Дисциплины";
                disciplinesHeaderRange.Style.Font.Bold = true;
                disciplinesHeaderRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                disciplinesHeaderRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                disciplinesHeaderRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkViolet);
                disciplinesHeaderRange.Style.Font.Color.SetColor(System.Drawing.Color.Black);
            }

            Dictionary<(int StudentId, int DisciplineId), (int Present, int Total)> presenceDict = new();
            var allPresences = await _presenceRepository.GetPresencesByDisciplinesAndGroupsAsync(
                disciplinesList.Select(d => d.Id).ToList(),
                sortedGroups.Select(g => g.Id).ToList()
            );
            presenceDict = allPresences
                .GroupBy(m => new { m.StudentId, m.DisciplineId })
                .ToDictionary(
                    g => (g.Key.StudentId, g.Key.DisciplineId),
                    g => (Present: g.Count(m => m.IsPresent == PresenceType.PRESENT), Total: g.Count())
                );
            int currentRow = 2;

            foreach (var group in sortedGroups)
            {
                var groupNameCell = worksheet.Cells[currentRow, 1];
                groupNameCell.Value = group.Name;
                groupNameCell.Style.Font.Bold = true;
                groupNameCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupNameCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                var groupDisciplineIds = group.Classes?
                    .Where(c => c.Discipline != null)
                    .Select(c => c.DisciplineId)
                    .Distinct()
                    .ToHashSet() ?? new HashSet<int>();

                for (int i = 0; i < disciplinesList.Count; i++)
                {
                    var currentDisc = disciplinesList[i];
                    var discCell = worksheet.Cells[currentRow, i + 2];

                    if (groupDisciplineIds.Contains(currentDisc.Id))
                    {
                        discCell.Value = currentDisc.Name;
                    }

                    discCell.Style.Font.Bold = true;
                    discCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    discCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                currentRow++;

                var groupStudents = students.Where(s => s.GroupId == group.Id).OrderBy(s => s.Name).ToList();
                foreach (var student in groupStudents)
                {
                    worksheet.Cells[currentRow, 1].Value = student.Name;
                    for (int i = 0; i < disciplinesList.Count; i++)
                    {
                        var disc = disciplinesList[i];
                        var cell = worksheet.Cells[currentRow, i + 2];

                        if (groupDisciplineIds.Contains(disc.Id))
                        {
                            if (presenceDict.TryGetValue((student.Id, disc.Id), out var stats))
                                cell.Value = $"{stats.Present}/{stats.Total}";
                            else
                                cell.Value = "0/0";
                        }
                        else
                        {
                            cell.Value = "-";
                        }
                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                    currentRow++;
                }
            }

            worksheet.Cells.AutoFitColumns();
            var modelRange = worksheet.Cells[1, 1, currentRow - 1, maxDisciplineCount + 1];
            modelRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            modelRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

            using var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            return stream.ToArray();
        }
    }
}
