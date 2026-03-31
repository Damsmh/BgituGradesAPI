using BgituGrades.Features;
using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Discipline
{
    public class CreateDisciplineRequest
    {
        [Required]
        public string? Name { get; set; }
    }

    public class UpdateDisciplineRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
    }

    public class DeleteDisciplineRequest
    {
        [Required]
        public int Id { get; set; }
    }

    public class GetDisciplineByGroupIdsRequest
    {
        [Required]
        public required CommaSeparatedIntArray GroupIds { get; set; }
    }
}
