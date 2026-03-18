using BgituGrades.Models.Mark;
using BgituGrades.Models.Presence;

namespace BgituGrades.Models.Student
{
    public class StudentResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int GroupId { get; set; }
    }

    public class ImportResult
    {
        public int ProcessedRows { get; set; }
        public int SkippedRows { get; set; }
        public HashSet<string> UnknownGroups { get; set; } = new();
    }
}
