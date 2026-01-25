using Grades.Entities;

namespace BgutuGrades.DTO
{
    public class ClassDTO
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }
}
