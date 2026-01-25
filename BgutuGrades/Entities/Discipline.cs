using BgutuGrades.Entities;

namespace Grades.Entities
{
    public class Discipline
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public ICollection<Class>? Classes { get; set; }
        public ICollection<Work>? Works { get; set; }
        public ICollection<Presence>? Presences { get; set; }
        public ICollection<Transfer>? Transfers { get; set; }
    }
}
