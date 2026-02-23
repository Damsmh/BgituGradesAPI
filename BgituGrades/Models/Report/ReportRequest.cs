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
        public ReportType Type { get; set; }
    }
}
