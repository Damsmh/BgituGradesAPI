namespace BgituGrades.Models.Report
{
    public class ProgressReportResponse
    {
        public string ReportId {  get; set; }
        public int Progress {  get; set; }
        public string Description {  get; set; }
    }

    public class ReadyReportResponse
    {
        public string ReportId { get; set; }
        public string Link { get; set; }
    }
}
