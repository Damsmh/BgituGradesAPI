using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Entities
{
    [Index(nameof(GroupId))]
    [Index(nameof(DisciplineId))]
    [Index(nameof(GroupId), nameof(DisciplineId))]
    public class Class
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public DateTime StartTime { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
        public Discipline? Discipline { get; set; }
        public Group? Group { get; set; }
    }
}
