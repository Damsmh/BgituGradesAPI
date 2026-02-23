using Asp.Versioning;
using BgituGrades.Models.Student;
using BgituGrades.Models.Work;
using BgituGrades.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace BgituGrades.Controllers
{
    [Route("api/work")]
    [ApiVersion("1.0")]
    [Obsolete("deprecated")]
    [ApiController]
    public class WorkController(IWorkService WorkService) : ControllerBase
    {
        private readonly IWorkService _workService = WorkService;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WorkResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<WorkResponse>>> GetWorks()
        {
            var works = await _workService.GetAllWorksAsync();
            return Ok(works);
        }

        [HttpPost]
        [ProducesResponseType(typeof(WorkResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<WorkResponse>> CreateWork([FromBody] CreateWorkRequest request)
        {
            var work = await _workService.CreateWorkAsync(request);
            return CreatedAtAction(nameof(GetWork), new { id = work.Id }, work);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WorkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WorkResponse>> GetWork([FromRoute] int id)
        {
            var work = await _workService.GetWorkByIdAsync(id);
            if (work == null)
                return NotFound(id);
            return Ok(work);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWork([FromBody] UpdateWorkRequest request)
        {
            var success = await _workService.UpdateWorkAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWork([FromQuery] DeleteWorkRequest request)
        {
            var success = await _workService.DeleteWorkAsync(request.Id);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }
    }
}
