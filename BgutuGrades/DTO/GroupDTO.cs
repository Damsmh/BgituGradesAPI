namespace BgutuGrades.DTO
{
    public class GroupDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Code { get; set; }
        public DateOnly StudyStartDate { get; set; }
        public DateOnly StudyEndDate { get; set; }
        public int StartWeekNumber { get; set; }
    }
}
