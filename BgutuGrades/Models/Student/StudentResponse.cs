using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;

namespace BgutuGrades.Models.Student
{
    public class StudentResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int GroupId { get; set; }
    }
}
