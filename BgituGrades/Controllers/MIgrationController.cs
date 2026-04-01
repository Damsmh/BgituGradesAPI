using Asp.Versioning;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BgituGrades.Controllers
{
    [Route("api")]
    [ApiVersion("2.0")]
    [Authorize(Policy = "Admin")]
    [ApiController]
    public class MIgrationController(IMigrationService migrationService) : ControllerBase
    {
        private readonly IMigrationService _migrationService = migrationService;

        [HttpDelete("clearDb")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(CancellationToken cancellationToken)
        {
            await _migrationService.DeleteAll(cancellationToken: cancellationToken);
            return NoContent();
        }

        [HttpPost("migrate")]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> MigrateAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _migrationService.ArchiveCurrentSemesterAsync(cancellationToken);
                return Ok("Архивация успешно завершена.");
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}
