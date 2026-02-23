using Asp.Versioning;
using BgituGrades.Data;
using BgituGrades.Models.Mark;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using BgituGrades.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Controllers
{
    [Route("api/mark")]
    [ApiController]
    public class MarkController(IMarkService MarkService, AppDbContext dbContext) : ControllerBase
    {
        private readonly IMarkService _markService = MarkService;
        private readonly AppDbContext _dbContext = dbContext;

        [HttpGet]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(IEnumerable<MarkResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetMarks()
        {
            var marks = await _markService.GetAllMarksAsync();
            return Ok(marks);
        }

        [HttpPost]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(MarkResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<MarkResponse>> CreateMark([FromBody] CreateMarkRequest request)
        {
            var mark = await _markService.CreateMarkAsync(request);
            return CreatedAtAction(nameof(GetMarkByDisciplineAndGroup), new { id = mark.Id }, mark);
        }

        [HttpGet("by_dId_gId")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(MarkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MarkResponse>> GetMarkByDisciplineAndGroup([FromQuery] GetMarksByDisciplineAndGroupRequest request)
        {
            var marks = await _markService.GetMarksByDisciplineAndGroupAsync(request);
            if (marks == null)
                return NotFound(new {disciplineId = request.DisciplineId, groupId = request.GroupId});
            return Ok(marks);
        }

        [HttpPut]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMark([FromBody] UpdateMarkRequest request)
        {
            var success = await _markService.UpdateMarkAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpPut("grade")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<GradeMarkResponse>> UpdateMarkGrade([FromQuery] UpdateMarkGradeRequest request, [FromBody] UpdateMarkRequest mark)
        {
            var existing = await _dbContext.Marks
                .FirstOrDefaultAsync(m => m.StudentId == request.StudentId && m.WorkId == mark.WorkId);

            if (existing != null)
            {
                existing.Value = mark.Value;
                existing.Date = mark.Date;
                existing.IsOverdue = mark.IsOverdue;
            }
            else
            {
                _dbContext.Marks.Add(new Mark
                {
                    StudentId = request.StudentId,
                    WorkId = mark.WorkId,
                    Value = mark.Value,
                    Date = mark.Date,
                    IsOverdue = mark.IsOverdue
                });
            }

            await _dbContext.SaveChangesAsync();

            return NoContent();

        }

        [HttpDelete]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMark([FromQuery] DeleteMarkByStudentAndWorkRequest request)
        {
            var success = await _markService.DeleteMarkByStudentAndWorkAsync(request);
            if (!success)
                return NotFound(request.WorkId);

            return NoContent();
        }
    }
}
