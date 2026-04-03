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
    public interface IArchivedReportService
    {
        Task<Guid> GenerateReportAsync(ArchivedReportRequest request, string connectionId, CancellationToken cancellationToken);
    }

    public class ArchivedReportService(
        IHubContext<ReportHub> hubContext,
        IDistributedCache cache,
        IServiceScopeFactory scopeFactory) : IArchivedReportService
    {
        public async Task<Guid> GenerateReportAsync(ArchivedReportRequest request, string connectionId, CancellationToken cancellationToken)
        {
            var reportId = Guid.NewGuid();
            await hubContext.Groups.AddToGroupAsync(connectionId, reportId.ToString());
            _ = Task.Run(() => GenerateWithProgress(reportId, request, cancellationToken), cancellationToken);
            return reportId;
        }

        private async Task GenerateWithProgress(Guid reportId, ArchivedReportRequest request, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<IReportSnapshotRepository>();

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));

            try
            {
                await SendProgress(reportId, 10, "Загрузка архивных данных...");

                var allSnapshots = await snapshotRepo.GetReportSnapshotsByYearAndSemesterAsync(request.Year, request.Semester, cancellationToken);

                var snapshots = allSnapshots
                    .Where(s => request.GroupIds == null || request.GroupIds.Contains(s.GroupId))
                    .Where(s => request.DisciplineIds == null || request.DisciplineIds.Contains(s.DisciplineId))
                    .Where(s => request.StudentIds == null || request.StudentIds.Contains(s.StudentId))
                    .ToList();

                if (snapshots.Count == 0)
                    throw new Exception($"Нет архивных данных за {request.Year} год, семестр {request.Semester}");

                await SendProgress(reportId, 40, "Генерация Excel файла...");

                var result = request.ReportType == ReportType.MARK
                    ? GenerateMarksExcel(snapshots)
                    : GeneratePresenceExcel(snapshots);

                await SendProgress(reportId, 80, "Сохранение...");
                await cache.SetAsync($"report_{reportId}", result.ExcelBytes!, cacheOptions);

                await hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("ReportReady", reportId.ToString(),
                        $"https://maxim.pamagiti.site/api/report/{reportId}/download",
                        result.Preview);
            }
            catch (Exception ex)
            {
                await hubContext.Clients.Group(reportId.ToString())
                    .SendAsync("Error", ex.Message, ex.StackTrace);
            }
        }

        private static TablePreview GenerateMarksExcel(List<ReportSnapshot> snapshots)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт успеваемости");
            var zebraColor = System.Drawing.Color.FromArgb(245, 245, 245);

            var byGroup = snapshots
                .GroupBy(s => (s.GroupId, s.GroupName))
                .OrderBy(g => g.Key.GroupName)
                .ToList();

            var preview = new ReportPreviewDto();
            int currentRow = 1;

            foreach (var group in byGroup)
            {
                var disciplines = group
                    .GroupBy(s => (s.DisciplineId, s.DisciplineName))
                    .OrderBy(d => d.Key.DisciplineName)
                    .ToList();

                var students = group
                    .GroupBy(s => (s.StudentId, s.StudentName))
                    .OrderBy(s => s.Key.StudentName)
                    .ToList();

                int maxCols = disciplines.Count;

                var groupRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                worksheet.Cells[currentRow, 1].Value = group.Key.GroupName;
                groupRange.Style.Font.Bold = true;
                groupRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                for (int i = 0; i < disciplines.Count; i++)
                {
                    var cell = worksheet.Cells[currentRow, i + 2];
                    cell.Value = disciplines[i].Key.DisciplineName;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                preview.Rows.Add(new PreviewRow
                {
                    IsGroupHeader = true,
                    Cells = [group.Key.GroupName, .. disciplines.Select(d => d.Key.DisciplineName)]
                });

                currentRow++;

                for (int sIdx = 0; sIdx < students.Count; sIdx++)
                {
                    var student = students[sIdx];
                    worksheet.Cells[currentRow, 1].Value = student.Key.StudentName;

                    if (sIdx % 2 != 0)
                    {
                        var rowRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                        rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(zebraColor);
                    }

                    var markByDiscipline = group
                        .Where(s => s.StudentId == student.Key.StudentId)
                        .ToDictionary(s => s.DisciplineId, s => s.Marks);

                    var previewCells = new List<string> { student.Key.StudentName };

                    for (int i = 0; i < disciplines.Count; i++)
                    {
                        var cell = worksheet.Cells[currentRow, i + 2];
                        var mark = markByDiscipline.GetValueOrDefault(disciplines[i].Key.DisciplineId, "0.0");

                        if (double.TryParse(mark, NumberStyles.Any, CultureInfo.InvariantCulture, out var markVal))
                        {
                            cell.Value = markVal;
                            cell.Style.Numberformat.Format = "0.0";
                        }
                        else
                        {
                            cell.Value = mark;
                        }

                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        previewCells.Add(mark);
                    }

                    preview.Rows.Add(new PreviewRow { IsGroupHeader = false, Cells = previewCells });
                    currentRow++;
                }
            }

            ApplyTableStyle(worksheet, currentRow);

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return new TablePreview { ExcelBytes = stream.ToArray(), Preview = preview };
        }

        private static TablePreview GeneratePresenceExcel(List<ReportSnapshot> snapshots)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Отчёт посещаемости");
            var zebraColor = System.Drawing.Color.FromArgb(245, 245, 245);

            var byGroup = snapshots
                .GroupBy(s => (s.GroupId, s.GroupName))
                .OrderBy(g => g.Key.GroupName)
                .ToList();

            var preview = new ReportPreviewDto();
            int currentRow = 1;

            foreach (var group in byGroup)
            {
                var disciplines = group
                    .GroupBy(s => (s.DisciplineId, s.DisciplineName))
                    .OrderBy(d => d.Key.DisciplineName)
                    .ToList();

                var students = group
                    .GroupBy(s => (s.StudentId, s.StudentName))
                    .OrderBy(s => s.Key.StudentName)
                    .ToList();

                int maxCols = disciplines.Count;

                var groupRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                worksheet.Cells[currentRow, 1].Value = group.Key.GroupName;
                groupRange.Style.Font.Bold = true;
                groupRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                groupRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                for (int i = 0; i < disciplines.Count; i++)
                {
                    var cell = worksheet.Cells[currentRow, i + 2];
                    cell.Value = disciplines[i].Key.DisciplineName;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                preview.Rows.Add(new PreviewRow
                {
                    IsGroupHeader = true,
                    Cells = [group.Key.GroupName, .. disciplines.Select(d => d.Key.DisciplineName)]
                });

                currentRow++;

                for (int sIdx = 0; sIdx < students.Count; sIdx++)
                {
                    var student = students[sIdx];
                    worksheet.Cells[currentRow, 1].Value = student.Key.StudentName;

                    if (sIdx % 2 != 0)
                    {
                        var rowRange = worksheet.Cells[currentRow, 1, currentRow, maxCols + 1];
                        rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        rowRange.Style.Fill.BackgroundColor.SetColor(zebraColor);
                    }

                    var presenceByDiscipline = group
                        .Where(s => s.StudentId == student.Key.StudentId)
                        .ToDictionary(s => s.DisciplineId, s => s.Presences);

                    var previewCells = new List<string> { student.Key.StudentName };

                    for (int i = 0; i < disciplines.Count; i++)
                    {
                        var cell = worksheet.Cells[currentRow, i + 2];
                        var presence = presenceByDiscipline.GetValueOrDefault(disciplines[i].Key.DisciplineId, "0/0");

                        cell.Value = presence;
                        cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        previewCells.Add(presence);
                    }

                    preview.Rows.Add(new PreviewRow { IsGroupHeader = false, Cells = previewCells });
                    currentRow++;
                }
            }

            ApplyTableStyle(worksheet, currentRow);

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return new TablePreview { ExcelBytes = stream.ToArray(), Preview = preview };
        }

        private static void ApplyTableStyle(ExcelWorksheet worksheet, int lastRow)
        {
            if (lastRow <= 1) return;
            var range = worksheet.Cells[1, 1, lastRow - 1, worksheet.Dimension.Columns];
            range.AutoFitColumns();
            range.Style.Border.Top.Style =
            range.Style.Border.Bottom.Style =
            range.Style.Border.Left.Style =
            range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            worksheet.View.FreezePanes(2, 2);
        }

        private Task SendProgress(Guid reportId, int percent, string message) =>
            hubContext.Clients.Group(reportId.ToString())
                .SendAsync("ReportProgress", reportId.ToString(), percent, message);
    }
}
