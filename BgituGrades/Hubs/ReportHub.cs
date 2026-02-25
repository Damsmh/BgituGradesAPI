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

        [Channel("hubs/report/GenerateReport")]
        [PublishOperation(typeof(ReportRequest), Summary = "Запросить формирование отчёта", OperationId = nameof(GenerateReport))]
        public async Task GenerateReport(ReportRequest request)
        {
            var connectionId = Context.ConnectionId;
            var reportId = await _reportService.GenerateReportAsync(request, connectionId);

            await Clients.Caller.SendAsync("ReportStarted", reportId);
        }


        [Channel("hubs/report/ReportStarted")]
        [SubscribeOperation(typeof(Guid), Summary = "Событие: Получение reportId (сразу после запроса)", OperationId = "ReportStarted")]
        public void ReportStarted(Guid reportId) { }

        [Channel("hubs/report/ReportProgress")]
        [SubscribeOperation(typeof(ProgressReportResponse), Summary = "Событие: Уведомление о прогрессе формирования (0-100%)", OperationId = "ReportProgress")]
        public void ReportProgress(ProgressReportResponse response) { }

        [Channel("hubs/report/ReportReady")]
        [SubscribeOperation(typeof(ReadyReportResponse), Summary = "Событие: Отчёт готов (содержит ссылку на скачивание)", OperationId = "ReportReady")]
        public void ReportReady(ReadyReportResponse response) { }
    }
}
