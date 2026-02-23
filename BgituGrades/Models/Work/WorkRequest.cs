using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Work
{
    public class CreateWorkRequest
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public DateOnly IssuedDate { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public string? Link { get; set; }
        [Required]
        public int DisciplineId { get; set; }
    }

    public class UpdateWorkRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public DateOnly IssuedDate { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        [Required]
        public int DisciplineId { get; set; }
    }

    public class DeleteWorkRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
