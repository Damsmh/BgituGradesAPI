using BgituGrades.DTO;

namespace BgituGrades.Models.Report
{
    public class ReportResponse
    {
        public required string ReportId { get; set; }
        public int Progress { get; set; }
        public required string Description { get; set; }
    }

    public class ReadyReportResponse
    {
        public required string ReportId { get; set; }
        public required string Link { get; set; }
        public ReportPreviewDto? Preview { get; set; }
    }

    public class PeriodResponse
    {
        public int Semester { get; set; }
        public int Year { get; set; }
    }
}
