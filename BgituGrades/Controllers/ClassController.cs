using Asp.Versioning;
using BgituGrades.Models.Class;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BgituGrades.Controllers
{
    [Route("api/class")]
    [ApiController]
    public class ClassController(IClassService ClassService) : ControllerBase
    {
        private readonly IClassService _classService = ClassService;

        [HttpGet]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [EndpointDescription("Больше не используется, т.к. произведён полный переход на SignalR")]
        [ProducesResponseType(typeof(IEnumerable<ClassDateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClassDateResponse>>> GetClasssDates([FromBody] GetClassDateRequest request)
        {
            var classDates = await _classService.GetClassDatesAsync(request);
            return Ok(classDates);
        }

        [HttpGet("markGrade")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [EndpointDescription("Больше не используется, т.к. произведён полный переход на SignalR")]
        [ProducesResponseType(typeof(IEnumerable<FullGradeMarkResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FullGradeMarkResponse>>>GetMarkGrade([FromQuery] GetClassDateRequest request)
        {
            var works = await _classService.GetMarksByWorksAsync(request);
            return Ok(works);
        }

        [HttpGet("presenceGrade")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [EndpointDescription("Больше не используется, т.к. произведён полный переход на SignalR")]
        [ProducesResponseType(typeof(IEnumerable<FullGradePresenceResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FullGradePresenceResponse>>> GetPresenceGrade([FromQuery] GetClassDateRequest request)
        {
            var classDates = await _classService.GetPresenceByScheduleAsync(request);
            return Ok(classDates);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ClassResponse>> CreateClass([FromBody] CreateClassRequest request)
        {
            var _class = await _classService.CreateClassAsync(request);
            return CreatedAtAction(nameof(GetClass), new { id = _class.Id }, _class);
        }

        [HttpGet("{id}")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClassResponse>> GetClass([FromRoute] int id)
        {
            var _class = await _classService.GetClassByIdAsync(id);
            if (_class == null)
                return NotFound(id);
            return Ok(_class);
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClass([FromQuery] int id)
        {
            var success = await _classService.DeleteClassAsync(id);
            if (!success)
                return NotFound(id);

            return NoContent();
        }
    }
}
