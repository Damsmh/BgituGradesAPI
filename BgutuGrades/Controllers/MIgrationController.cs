using BgutuGrades.Services;
using Microsoft.AspNetCore.Mvc;


namespace BgutuGrades.Controllers
{
    [Route("api/destroyUkraine")]
    [ApiController]
    public class MIgrationController(IMigrationService migrationService) : ControllerBase
    {
        private readonly IMigrationService _migrationService = migrationService;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete()
        {
            await _migrationService.DeleteAll();
            return NoContent();
        }
    }
}
