using Asp.Versioning;
using BgituGrades.Models.Discipline;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BgituGrades.Controllers
{
    [Route("api/discipline")]
    [ApiController]
    public class DisciplineController(IDisciplineService DisciplineService) : ControllerBase
    {
        private readonly IDisciplineService _disciplineService = DisciplineService;

        [HttpGet("all")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<DisciplineResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DisciplineResponse>>> GetDisciplines()
        {
            var Disciplines = await _disciplineService.GetAllDisciplinesAsync();
            return Ok(Disciplines);
        }

        [HttpGet]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<DisciplineResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DisciplineResponse>>> GetDisciplinesByGroupId([FromQuery] int groupId)
        {
            var Disciplines = await _disciplineService.GetDisciplineByGroupIdAsync(groupId);
            return Ok(Disciplines);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(DisciplineResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<DisciplineResponse>> CreateDiscipline([FromBody] CreateDisciplineRequest request)
        {
            var Discipline = await _disciplineService.CreateDisciplineAsync(request);
            return CreatedAtAction(nameof(GetDiscipline), new { id = Discipline.Id }, Discipline);
        }

        [HttpGet("{id}")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(DisciplineResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DisciplineResponse>> GetDiscipline([FromRoute] int id)
        {
            var Discipline = await _disciplineService.GetDisciplineByIdAsync(id);
            if (Discipline == null)
                return NotFound(id);
            return Ok(Discipline);
        }

        [HttpPut]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDiscipline([FromBody] UpdateDisciplineRequest request)
        {
            var success = await _disciplineService.UpdateDisciplineAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDiscipline([FromQuery] DeleteDisciplineRequest request)
        {
            var success = await _disciplineService.DeleteDisciplineAsync(request.Id);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }
    }
}
