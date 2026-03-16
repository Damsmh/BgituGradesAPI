namespace BgituGrades.Entities
{
    public class Work
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateOnly IssuedDate { get; set; }
        public string? Description { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
        public Discipline? Discipline { get; set; }
        public Group? Group { get; set; }
        public ICollection<Mark>? Marks { get; set; }
    }
}
