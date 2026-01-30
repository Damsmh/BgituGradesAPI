namespace BgutuGrades.Models.Key
{
    public class KeyResponse
    {
        public string? Key { get; set; }
    }

    public class SharedKeyResponse : KeyResponse
    {
        public string? Link { get; set; }
    }
}
