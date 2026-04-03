using Asp.Versioning;
using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Models.Mark;
using BgituGrades.Models.Student;
using BgituGrades.Services;
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
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(IEnumerable<MarkResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MarkResponse>>> GetMarks(CancellationToken cancellationToken)
        {
            var marks = await _markService.GetAllMarksAsync(cancellationToken: cancellationToken);
            return Ok(marks);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(MarkResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<MarkResponse>> CreateMark([FromBody] CreateMarkRequest request, CancellationToken cancellationToken)
        {
            var mark = await _markService.CreateMarkAsync(request, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetMarkByDisciplineAndGroup), new { id = mark.Id }, mark);
        }

        [HttpGet("by_dId_gId")]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(MarkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MarkResponse>> GetMarkByDisciplineAndGroup([FromQuery] GetMarksByDisciplineAndGroupRequest request, CancellationToken cancellationToken)
        {
            var marks = await _markService.GetMarksByDisciplineAndGroupAsync(request, cancellationToken: cancellationToken);
            if (marks == null)
                return NotFound(new { disciplineId = request.DisciplineId, groupId = request.GroupId });
            return Ok(marks);
        }

        [HttpPut]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMark([FromBody] UpdateMarkRequest request, CancellationToken cancellationToken)
        {
            var success = await _markService.UpdateMarkAsync(request, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpPut("grade")]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<GradeMarkResponse>> UpdateMarkGrade([FromQuery] UpdateMarkGradeRequest request, [FromBody] UpdateMarkRequest mark, CancellationToken cancellationToken)
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
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMark([FromQuery] DeleteMarkByStudentAndWorkRequest request, CancellationToken cancellationToken)
        {
            var success = await _markService.DeleteMarkByStudentAndWorkAsync(request, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.WorkId);

            return NoContent();
        }
    }
}
