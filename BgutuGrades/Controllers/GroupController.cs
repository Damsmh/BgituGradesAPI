using BgutuGrades.Models.Group;
using BgutuGrades.Models.Student;
using BgutuGrades.Services;
using Microsoft.AspNetCore.Mvc;

namespace BgutuGrades.Controllers
{
    [Route("api/group")]
    [ApiController]
    public class GroupController(IGroupService groupService) : ControllerBase
    {
        private readonly IGroupService _groupService = groupService;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GroupResponse>>> GetGroups(
            [FromQuery] GetGroupsByDisciplineRequest request)
        {
            var groups = await _groupService.GetGroupsByDisciplineAsync(request.DisciplineId);
            return Ok(groups);
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GroupResponse>>> GetAllGroups()
        {
            var groups = await _groupService.GetAllAsync();
            return Ok(groups);
        }

        [HttpPost]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupResponse>> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var group = await _groupService.CreateGroupAsync(request);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupResponse>> GetGroup([FromRoute] int id)
        {
            var group = await _groupService.GetGroupByIdAsync(id);
            if (group == null)
                return NotFound(id);
            return Ok(group);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(UpdateGroupRequest), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequest request)
        {
            var success = await _groupService.UpdateGroupAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteGroup([FromQuery] DeleteGroupRequest request)
        {
            var success = await _groupService.DeleteGroupAsync(request.Id);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }
    }
}
