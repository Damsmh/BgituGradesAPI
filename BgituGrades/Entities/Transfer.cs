using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Entities
{
    [Index(nameof(GroupId), nameof(DisciplineId))]
    public class Transfer
    {
        public int Id { get; set; }
        public DateOnly OriginalDate { get; set; }
        public DateOnly NewDate { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
        public Discipline? Discipline { get; set; }
        public Group? Group { get; set; }
    }
}
