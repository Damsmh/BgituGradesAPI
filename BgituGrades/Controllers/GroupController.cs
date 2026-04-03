using Asp.Versioning;
using BgituGrades.Models.Group;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BgituGrades.Controllers
{
    [Route("api/group")]
    [ApiController]
    public class GroupController(IGroupService groupService) : ControllerBase
    {
        private readonly IGroupService _groupService = groupService;

        [HttpGet]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GroupResponse>>> GetGroups(
            [FromQuery] GetGroupsByDisciplineRequest request, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetGroupsByDisciplineAsync(request.DisciplineId, cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpGet("courses")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<CourseReponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CourseReponse>>> GetCoursesByPeriod(CancellationToken cancellationToken)
        {
            var courses = await _groupService.GetCoursesAsync(cancellationToken: cancellationToken);
            return Ok(courses);
        }

        [HttpGet("archived/courses")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<CourseReponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CourseReponse>>> GetArchivedCoursesByPeriod(
            [FromQuery] GetByPeriodRequest request, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetArchivedCoursesByPeriodAsync(request, cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpGet("all")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GroupResponse>>> GetAllGroups(CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetAllAsync(cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpGet("archived")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<ArchivedGroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ArchivedGroupResponse>>> GetArchivedGroups([FromQuery] GetByPeriodRequest request, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetArchivedGroupsByPeriodAsync(semester: request.Semester, year: request.Year, cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpGet("archived/by_courses")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<ArchivedGroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ArchivedGroupResponse>>> GetArchivedGroups([FromQuery] GetArchivedByCoursesRequest request, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetArchivedGroupsByCoursesAndPeriodAsync(request, cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpGet("by_courses")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<GroupResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GroupResponse>>> GetGroupsByCourses([FromQuery] GetByCoursesRequest request, CancellationToken cancellationToken)
        {
            var groups = await _groupService.GetGroupsByCoursesAsync(request.Courses!, cancellationToken: cancellationToken);
            return Ok(groups);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<GroupResponse>> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var group = await _groupService.CreateGroupAsync(request, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }

        [HttpGet("{id}")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GroupResponse>> GetGroup([FromRoute] int id, CancellationToken cancellationToken)
        {
            var group = await _groupService.GetGroupByIdAsync(id, cancellationToken: cancellationToken);
            if (group == null)
                return NotFound(id);
            return Ok(group);
        }

        [HttpPut]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(UpdateGroupRequest), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupRequest request, CancellationToken cancellationToken)
        {
            var success = await _groupService.UpdateGroupAsync(request, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteGroup([FromQuery] DeleteGroupRequest request, CancellationToken cancellationToken)
        {
            var success = await _groupService.DeleteGroupAsync(request.Id, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }
    }
}
