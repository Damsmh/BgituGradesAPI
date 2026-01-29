using BgutuGrades.Models.Class;
using BgutuGrades.Models.Student;
using BgutuGrades.Services;
using Grades.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BgutuGrades.Controllers
{
    [Route("api/class")]
    [ApiController]
    public class ClassController(IClassService ClassService) : ControllerBase
    {
        private readonly IClassService _classService = ClassService;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ClassDateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ClassDateResponse>>> GetClasssDates([FromBody] GetClassDateRequest request)
        {
            var classDates = await _classService.GetClassDatesAsync(request);
            return Ok(classDates);
        }

        [HttpGet("markGrade")]
        [ProducesResponseType(typeof(IEnumerable<FullGradeMarkResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FullGradeMarkResponse>>>GetMarkGrade([FromQuery] GetClassDateRequest request)
        {
            var classDates = await _classService.GetMarksByScheduleAsync(request);
            return Ok(classDates);
        }

        [HttpGet("presenceGrade")]
        [ProducesResponseType(typeof(IEnumerable<FullGradePresenceResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FullGradePresenceResponse>>> GetPresenceGrade([FromQuery] GetClassDateRequest request)
        {
            var classDates = await _classService.GetPresenceByScheduleAsync(request);
            return Ok(classDates);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<ClassResponse>> CreateClass([FromBody] CreateClassRequest request)
        {
            var _class = await _classService.CreateClassAsync(request);
            return CreatedAtAction(nameof(GetClass), new { id = _class.Id }, _class);
        }

        [HttpGet("{id}")]
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
