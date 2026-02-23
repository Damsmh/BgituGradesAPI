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

        [HttpGet]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(IEnumerable<KeyResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<KeyResponse>>> GetKeys()
        {
            var Keys = await _keyService.GetKeysAsync();
            return Ok(Keys);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> CreateKey(CreateKeyRequest request)
        {
            var key = await _keyService.GenerateKeyAsync(request.Role);
            return CreatedAtAction(nameof(GetKey), new { key = key.Key }, key);
        }

        [HttpGet("{key}")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> GetKey([FromRoute] string key)
        {
            var storedKey = await _keyService.GetKeyAsync(key);
            return Ok(storedKey);
        }

        [HttpGet("shared")]
        [Authorize(Policy = "Edit")]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(SharedKeyResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<KeyResponse>> CreateSharedKey(int groupId, int disciplineId)
        {
            var key = await _keyService.GenerateKeyAsync(Role.STUDENT);
            var response = new SharedKeyResponse
            {
                Link = $"{Request.Scheme}://{Request.Host}/visit?key={key.Key}"
            };
            return Ok(response);
        }

        [HttpDelete]
        [Authorize(Policy = "Admin")]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteKey([FromQuery] DeleteKeyRequest request)
        {
            var success = await _keyService.DeleteKeyAsync(request.Key);
            if (!success)
                return NotFound(request.Key);

            return NoContent();
        }
    }
}
