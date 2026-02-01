using Grades.Entities;

namespace BgutuGrades.Models.Presence
{
    public class PresenceResponse
    {
        public int Id { get; set; }
        public PresenceType IsPresent { get; set; }
        public DateOnly Date { get; set; }
        public int DisciplineId { get; set; }
        public int StudentId { get; set; }
    }

    public class GradePresenceResponse
    {
        public int ClassId { get; set; }
        public ClassType ClassType { get; set; }
        public PresenceType IsPresent { get; set; }
        public DateOnly Date { get; set; }
    }
}
