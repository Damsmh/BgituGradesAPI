using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Work
{
    public class WorkResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateOnly IssuedDate { get; set; }
        public string? Description { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }
}
