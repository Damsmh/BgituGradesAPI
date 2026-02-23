using AspNetCore.Authentication.ApiKey;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace BgituGrades.Entities
{
    public class ApiKey : IApiKey
    {
        public string Key { get; set; }
        public string OwnerName { get; set; }
        public string Role { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [NotMapped]
        public IReadOnlyCollection<Claim> Claims => [new Claim(ClaimTypes.Role, Role ?? "STUDENT")];
    }
}
