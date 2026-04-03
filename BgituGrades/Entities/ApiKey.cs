using AspNetCore.Authentication.ApiKey;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace BgituGrades.Entities
{
    public class ApiKey : IApiKey
    {
        public required string Key { get; set; }
        public string? StoredHash { get; set; }
        public string? LookupHash { get; set; }
        public required string OwnerName { get; set; }
        public string? Role { get; set; }
        public int? GroupId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [NotMapped]
        public IReadOnlyCollection<Claim> Claims
        {
            get
            {
                var claims = new List<Claim> { new(ClaimTypes.Role, Role ?? "STUDENT") };

                if (GroupId.HasValue)
                {
                    claims.Add(new Claim("group_id", GroupId.Value.ToString()));
                }
                return claims.AsReadOnly();
            }
        }
    }
}
