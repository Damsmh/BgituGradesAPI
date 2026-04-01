namespace BgituGrades.Entities
{
    public class ReportSnapshot
    {
        public int Id { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }

        public int StudentId { get; set; }
        public int OfficialStudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;

        public int DisciplineId { get; set; }
        public string DisciplineName { get; set; } = string.Empty;

        public string Presences { get; set; } = "0/0";
        public string Marks { get; set; } = "0.0";
    }
}
