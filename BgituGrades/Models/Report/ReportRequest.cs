using BgituGrades.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BgituGrades.Models.Report
{
    public class ReportRequest
    {
        [JsonIgnore]
        public string Host { get; set; } = string.Empty;
        public int[]? GroupIds { get; set; }
        public int[]? DisciplineIds { get; set; }
        public int[]? StudentIds { get; set; }
        [Required]
        public ReportType ReportType { get; set; }
    }

    public class ArchivedReportRequest
    {
        [JsonIgnore]
        public string Host { get; set; } = string.Empty;
        [Required]
        public int Year { get; set; }
        [Required]
        public int Semester { get; set; }
        public int[]? GroupIds { get; set; }
        public int[]? DisciplineIds { get; set; }
        public int[]? StudentIds { get; set; }
        [Required]
        public ReportType ReportType { get; set; }
    }
}
