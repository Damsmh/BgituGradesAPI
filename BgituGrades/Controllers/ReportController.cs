using Asp.Versioning;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace BgituGrades.Controllers
{
    [ApiController]
    [Route("api/report")]
    public class ReportController(
        IReportService reportService,
        IDistributedCache cache,
        ILogger<ReportController> logger) : ControllerBase
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<ReportController> _logger = logger;


        [HttpGet("{reportId}/download")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        public async Task<IActionResult> DownloadReport(Guid reportId)
        {
            byte[] excelBytes = await _cache.GetAsync($"report_{reportId}");

            if (excelBytes == null)
            {
                return NotFound("Отчет не найден или срок его хранения истек.");
            }
            
            var fileName = $"отчет_{reportId:N}.xlsx";
            

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
