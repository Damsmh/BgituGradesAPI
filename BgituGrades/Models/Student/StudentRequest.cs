using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Student
{
    public class NotFoundResponse
    {
        public int Id { get; set; }
    }
    public class GetStudentsByGroupRequest
    {
        [Required]
        public int GroupId { get; set; }
    }

    public class CreateStudentRequest
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public int GroupId { get; set; }
    }

    public class UpdateStudentRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        [Required]
        public int GroupId { get; set; }
    }

    public class DeleteStudentRequest
    {
        [Required]
        public int Id { get; set; }
    }
}
