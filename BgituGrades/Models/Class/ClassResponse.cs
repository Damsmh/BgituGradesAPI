using BgituGrades.Entities;
using BgituGrades.Models.Mark;
using BgituGrades.Models.Presence;
using BgituGrades.Models.Student;

namespace BgituGrades.Models.Class
{
    public class ClassResponse
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public DateTime StartTime { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }

    public class ClassDateResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public ClassType ClassType { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class FullGradeMarkResponse
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public List<GradeMarkResponse>? Marks { get; set; }
    }

    public class FullGradePresenceResponse
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public List<GradePresenceResponse>? Presences { get; set; }
    }
}
