using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;
using BgutuGrades.Models.Student;
using Grades.Entities;

namespace BgutuGrades.Models.Class
{
    public class ClassResponse
    {
        public int Id { get; set; }
        public int WeekDay { get; set; }
        public int Weeknumber { get; set; }
        public ClassType Type { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }

    public class ClassDateResponse
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public ClassType ClassType { get; set; }
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
