namespace Grades.Entities
{
    public class Transfer
    {
        public int Id { get; set; }
        public DateOnly OriginalDate { get; set; }
        public DateOnly NewDate { get; set; }
        public Discipline? Discipline { get; set; }
        public Group? Group { get; set; }
    }
}
