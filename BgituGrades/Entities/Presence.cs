namespace BgituGrades.Entities
{
    public class Presence
    {
        public int Id { get; set; }
        public PresenceType IsPresent { get; set; }
        public DateOnly Date { get; set; }
        public int DisciplineId { get; set; }
        public int StudentId { get; set; }
        public Discipline? Discipline { get; set; }
        public Student? Student { get; set; }
    }
}
