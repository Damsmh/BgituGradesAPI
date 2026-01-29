namespace BgutuGrades.Models.Mark
{
    public class MarkResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public string? Value { get; set; }
        public bool IsOverdue { get; set; }
        public int StudentId { get; set; }
        public int WorkId { get; set; }
    }

    public class GradeMarkResponse
    {
        public int ClassId { get; set; }
        public DateOnly Date { get; set; }
        public string? Value { get; set; }
    }
}
