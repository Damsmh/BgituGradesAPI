using BgutuGrades.Models.Key;
using BgutuGrades.Models.Student;
using BgutuGrades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace BgutuGrades.Controllers
{
    [Route("api/key")]
    [ApiController]
    public class KeyController(IKeyService keyService) : ControllerBase
    {
        private readonly IKeyService _keyService = keyService;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<KeyResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<KeyResponse>>> GetKeys()
        {
            var Keys = await _keyService.GetKeysAsync();
            return Ok(Keys);
        }

        [HttpPost]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> CreateKey(CreateKeyRequest request)
        {
            var key = await _keyService.GenerateKeyAsync(request.Role);
            return CreatedAtAction(nameof(GetKey), new { key = key.Key }, key);
        }

        [HttpGet("{key}")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> GetKey([FromRoute] string key)
        {
            var storedKey = await _keyService.GetKeyAsync(key);
            return Ok(storedKey);
        }

        [HttpPost("shared")]
        [ProducesResponseType(typeof(KeyResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<KeyResponse>> CreateSharedKey()
        {
            var key = await _keyService.GenerateKeyAsync(Entities.Role.STUDENT);
            var response = new SharedKeyResponse
            {
                Key = key.Key,
                Link = $"{Request.Scheme}://{Request.Host}/api/grades?Key={key.Key}"
            };
            return CreatedAtAction(nameof(GetKey), new { key = key.Key }, key);
        }

        [HttpDelete]
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
