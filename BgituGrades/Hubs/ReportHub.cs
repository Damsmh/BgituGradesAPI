using BgituGrades.Models.Report;
using BgituGrades.Services;
using Microsoft.AspNetCore.SignalR;
using Saunter.Attributes;

namespace BgituGrades.Hubs
{
    [AsyncApi]
    public class ReportHub(IReportService reportService) : Hub
    {
        private readonly IReportService _reportService = reportService;
        public async Task GenerateReport(ReportRequest request)
        {
            var connectionId = Context.ConnectionId;
            var reportId = await _reportService.GenerateReportAsync(request, connectionId);

            await Clients.Caller.SendAsync("ReportStarted", reportId);
        }
    }
}
