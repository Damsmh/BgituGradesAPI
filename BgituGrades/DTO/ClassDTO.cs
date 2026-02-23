using BgituGrades.Entities;

namespace BgituGrades.DTO
{
    public class ClassDTO
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public DateTime StartTime { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }

    public class ClassDateDTO {
        public DateOnly Date { get; set; }
        public ClassType ClassType { get; set; }
        public DateTime StartTime { get; set; }
    }

}
