using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Transfer
{
    public class CreateTransferRequest
    {
        [Required]
        public DateOnly OriginalDate { get; set; }
        [Required]
        public DateOnly NewDate { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }

    public class UpdateTransferRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public DateOnly OriginalDate { get; set; }
        [Required]
        public DateOnly NewDate { get; set; }
        [Required]
        public int DisciplineId { get; set; }
        [Required]
        public int GroupId { get; set; }
    }
}
