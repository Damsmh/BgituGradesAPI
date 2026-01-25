using Grades.Entities;

namespace BgutuGrades.DTO
{
    public class PresenceDTO
    {
        public int Id { get; set; }
        public PresenceType IsPresent { get; set; }
        public DateOnly Date { get; set; }
        public int DisciplineId { get; set; }
        public int StudentId { get; set; }
    }
}
