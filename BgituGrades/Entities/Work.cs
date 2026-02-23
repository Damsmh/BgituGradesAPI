namespace BgituGrades.Entities
{
    public class Work
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateOnly IssuedDate { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
        public int DisciplineId { get; set; }
        public Discipline? Discipline { get; set; }
        public ICollection<Mark>? Marks { get; set; }
    }
}
