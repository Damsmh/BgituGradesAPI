using BgutuGrades.Models.Student;
using BgutuGrades.Models.Discipline;
using BgutuGrades.Services;
using Microsoft.AspNetCore.Mvc;

namespace BgutuGrades.Controllers
{
    [Route("api/discipline")]
    [ApiController]
    public class DisciplineController(IDisciplineService DisciplineService) : ControllerBase
    {
        private readonly IDisciplineService _disciplineService = DisciplineService;

        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<DisciplineResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DisciplineResponse>>> GetDisciplines()
        {
            var Disciplines = await _disciplineService.GetAllDisciplinesAsync();
            return Ok(Disciplines);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DisciplineResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DisciplineResponse>>> GetDisciplinesByGroupId([FromQuery] int groupId)
        {
            var Disciplines = await _disciplineService.GetDisciplineByGroupIdAsync(groupId);
            return Ok(Disciplines);
        }

        [HttpPost]
        [ProducesResponseType(typeof(DisciplineResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<DisciplineResponse>> CreateDiscipline([FromBody] CreateDisciplineRequest request)
        {
            var Discipline = await _disciplineService.CreateDisciplineAsync(request);
            return CreatedAtAction(nameof(GetDiscipline), new { id = Discipline.Id }, Discipline);
        }

        [HttpGet("{id}")]
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
