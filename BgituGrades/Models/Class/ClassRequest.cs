using BgituGrades.Entities;
using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Class
{
    public class GetClassDateRequest
    {
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }

    public class CreateClassRequest
    {
        [Required]
        public int WeekDay { get; set; }
        [Required]
        public int Weeknumber { get; set; }
        [Required]
        public ClassType Type { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }
}
