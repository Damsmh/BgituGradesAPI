namespace BgituGrades.Entities
{
    public class Mark
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string? Value { get; set; }
        public bool IsOverdue { get; set; }
        public int StudentId { get; set; }
        public int WorkId { get; set; }
        public Student? Student { get; set; }
        public Work? Work { get; set; }
    }
}
