using BgituGrades.Entities;
using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Report
{
    public class ReportRequest
    {
        public int[]? GroupIds { get; set; }
        public int[]? DisciplineIds { get; set; }
        public int[]? StudentIds { get; set; }
        [Required]
        public ReportType ReportType { get; set; }
    }

    public class ArchivedReportRequest
    {
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
