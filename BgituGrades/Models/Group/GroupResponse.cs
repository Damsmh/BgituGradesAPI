using BgituGrades.Entities;

namespace BgituGrades.Models.Group
{
    public class GroupResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateOnly StudyStartDate { get; set; }
        public DateOnly StudyEndDate { get; set; }
        public int StartWeekNumber { get; set; }
        public SubGroup? SubGroup { get; set; }
    }
}
