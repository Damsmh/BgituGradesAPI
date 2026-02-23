using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Group
{
    public class GetGroupByIdRequest
    {
        [Required]
        public int Id { get; set; }
    }
    public class GetGroupsByDisciplineRequest
    {
        [Required]
        public int DisciplineId { get; set; }
    }

    public class CreateGroupRequest
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public DateOnly StudyStartDate { get; set; }
        [Required]
        public DateOnly StudyEndDate { get; set; }
        [Required]
        public int StartWeekNumber { get; set; }
    }

    public class UpdateGroupRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateOnly StudyStartDate { get; set; }
        [Required]
        public DateOnly StudyEndDate { get; set; }
        [Required]
        public int StartWeekNumber { get; set; }
    }

    public class DeleteGroupRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
