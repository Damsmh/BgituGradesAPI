using System.Globalization;

namespace BgituGrades.Entities
{
    public class Group
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateOnly StudyStartDate { get; set; }
        public DateOnly StudyEndDate { get; set; }
        public int StartWeekNumber { get; set; }
        public ICollection<Student>? Students { get; set; }
        public ICollection<Class>? Classes { get; set; }
        public ICollection<Transfer>? Transfers { get; set; }
    }
}
