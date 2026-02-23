namespace BgituGrades.Models.Key
{
    public class KeyResponse
    {
        public string? Key { get; set; }
        public string? Role { get; set; }
        public string? OwnerName { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class SharedKeyResponse
    {
        public string? Link { get; set; }
    }
}
