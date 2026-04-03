using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Entities
{
    [Index(nameof(StudentId), nameof(DisciplineId), nameof(Date))]
    [Index(nameof(DisciplineId))]
    public class Presence
    {
        public int Id { get; set; }
        public PresenceType IsPresent { get; set; }
        public DateOnly Date { get; set; }
        public int ClassId { get; set; }
        public int DisciplineId { get; set; }
        public int StudentId { get; set; }
        public Class? Class { get; set; }
        public Discipline? Discipline { get; set; }
        public Student? Student { get; set; }

    }
}
