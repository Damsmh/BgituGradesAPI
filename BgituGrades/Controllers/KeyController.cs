using Asp.Versioning;
using BgituGrades.Entities;
using BgituGrades.Models.Key;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BgituGrades.Controllers
{
    [Route("api/key")]
    [ApiController]
    public class KeyController(IKeyService keyService) : ControllerBase
    {
        private readonly IKeyService _keyService = keyService;

        [HttpGet("all")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<KeyResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<KeyResponse>>> GetKeys(CancellationToken cancellationToken)
        {
            var Keys = await _keyService.GetKeysAsync(cancellationToken: cancellationToken);
            return Ok(Keys);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> CreateKey(CreateKeyRequest request, CancellationToken cancellationToken)
        {
            var key = await _keyService.GenerateKeyAsync(request.Role, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetKey), new { key = key.Key }, key);
        }

        [HttpGet()]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<KeyResponse>> GetKey([FromHeader(Name = "key")] string key, CancellationToken cancellationToken)
        {
            var storedKey = await _keyService.GetKeyAsync(key, cancellationToken: cancellationToken);
            return Ok(storedKey);
        }

        [HttpGet("shared")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [Authorize(Policy = "Edit")]
        [ProducesResponseType(typeof(SharedKeyResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<KeyResponse>> CreateSharedKey(int groupId, int disciplineId, CancellationToken cancellationToken)
        {
            var key = await _keyService.GenerateKeyAsync(Role.STUDENT, cancellationToken: cancellationToken);
            var response = new SharedKeyResponse
            {
                Link = $"{Request.Scheme}://{Request.Host}/visit?key={key.Key}"
            };
            return Ok(response);
        }

        [HttpGet("shared")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        [ProducesResponseType(typeof(SharedKeyResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<KeyResponse>> CreateSharedKeyV2(CancellationToken cancellationToken)
        {
            var key = await _keyService.GenerateKeyAsync(Role.STUDENT, cancellationToken: cancellationToken);
            var response = new SharedKeyResponse
            {
                Link = $"{Request.Scheme}://{Request.Host}/visit?key={key.Key}"
            };
            return Ok(response);
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteKey([FromQuery] DeleteKeyRequest request, CancellationToken cancellationToken)
        {
            var success = await _keyService.DeleteKeyAsync(request.Key, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Key);

            return NoContent();
        }
    }
}
