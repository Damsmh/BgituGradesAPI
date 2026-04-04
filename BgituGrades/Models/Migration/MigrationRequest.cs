using BgituGrades.Models.Class;
using BgituGrades.Models.Discipline;
using BgituGrades.Models.Group;
using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Migration
{
    public class ScheduleImportRequest
    {
        [Required]
        public required List<CreateGroupRequest> Groups { get; set; }
        [Required]
        public required List<CreateDisciplineRequest> Disciplines { get; set; }
        [Required]
        public required List<CreateClassRequest> Pairs { get; set; }
    }
}
