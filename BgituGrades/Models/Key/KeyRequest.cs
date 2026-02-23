using BgituGrades.Entities;
using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Key
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
