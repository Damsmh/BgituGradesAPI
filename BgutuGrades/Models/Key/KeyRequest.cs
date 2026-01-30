using BgutuGrades.Entities;
using System.ComponentModel.DataAnnotations;

namespace BgutuGrades.Models.Key
{
    public class DeleteKeyRequest
    {
        [Required]
        public string? Key { get; set; }
    }

    public class CreateKeyRequest
    {
        [Required]
        public Role Role { get; set; }
    }
}
