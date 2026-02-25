using Asp.Versioning;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BgituGrades.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class ReportController(
        IReportService reportService,
        IMemoryCache cache,
        ILogger<ReportController> logger) : ControllerBase
    {
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<ReportController> _logger = logger;


        [HttpGet("{reportId}/download")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        public async Task<IActionResult> DownloadReport(Guid reportId)
        {
            if (!_cache.TryGetValue($"report_{reportId}", out byte[]? excelBytes))
            {
                _logger.LogWarning("Отчет {reportId} не найден в кэше", reportId);
                return NotFound("Отчет не найден");
            }

            var fileName = $"отчет_{reportId:N}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
