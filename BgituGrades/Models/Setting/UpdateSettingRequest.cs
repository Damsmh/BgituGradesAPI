using System.ComponentModel.DataAnnotations;

namespace BgituGrades.Models.Setting
{
    public class UpdateSettingRequest
    {
        [Required]
        public string? CalendarUrl { get; set; }
    }
}
