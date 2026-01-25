using Grades.Entities;

namespace BgutuGrades.Entities
{
    public class Class
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public Discipline? Discipline { get; set; }
        public Group? Group { get; set; }
    }
}
