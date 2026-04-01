namespace BgituGrades.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public int OfficialId { get; set; }
        public string? Name { get; set; }
        public int GroupId { get; set; }
        public int OfficialGroupId { get; set; }
        public Group? Group { get; set; }
        public ICollection<Presence>? Presences { get; set; }
        public ICollection<Mark>? Marks { get; set; }
    }
}
