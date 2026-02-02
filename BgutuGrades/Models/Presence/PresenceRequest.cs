using Grades.Entities;
using System.ComponentModel.DataAnnotations;

namespace BgutuGrades.Models.Presence
{
    public class CreatePresenceRequest
    {
        [Required]
        public PresenceType IsPresent { get; set; }
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int StudentId { get; set; }
    }

    public class UpdatePresenceRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public PresenceType IsPresent { get; set; }
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int StudentId { get; set; }
    }

    public class UpdatePresenceGradeRequest
    {
        [Required]
        public int ClassId { get; set; }
        [Required]
        public PresenceType IsPresent { get; set; }
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int StudentId { get; set; }
    }

    public class DeletePresenceByStudentAndDateRequest
    {
        [Required]
        public int StudentId { get; set; }
        [Required]
        public DateOnly Date { get; set; }
    }

    public class GetPresenceByDisciplineAndGroupRequest
    {
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }
}
