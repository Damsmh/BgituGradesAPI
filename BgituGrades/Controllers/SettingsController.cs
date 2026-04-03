using Asp.Versioning;
using BgituGrades.Models.Setting;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace BgituGrades.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class SettingsController(ISettingService settingService) : ControllerBase
    {
        private readonly ISettingService _settingService = settingService;

        [HttpGet]
        [ApiVersion("2.0")]
        public async Task<ActionResult<SettingResponse>> GetSettings(CancellationToken cancellationToken)
        {
            var settings = await _settingService.GetSettingsAsync(cancellationToken: cancellationToken);
            return Ok(settings);
        }


        [HttpPut]
        [ApiVersion("2.0")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
        {
            await _settingService.UpdateSettingAsync(request, cancellationToken: cancellationToken);
            return NoContent();
        }
    }
}
