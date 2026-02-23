using Asp.Versioning;
using BgituGrades.Data;
using BgituGrades.Models.Presence;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using BgituGrades.Models.Class;
using BgituGrades.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BgituGrades.Controllers
{
    [ApiVersion("1.0")]
    [Obsolete("deprecated")]
    [Description("Используйте SignalR")]
    [Route("api/presence")]
    [ApiController]
    public class PresenceController(IPresenceService PresenceService, AppDbContext dbContext) : ControllerBase
    {
        private readonly IPresenceService _presenceService = PresenceService;
        private readonly AppDbContext _dbContext = dbContext;

        [HttpGet]

        [ProducesResponseType(typeof(IEnumerable<PresenceResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PresenceResponse>>> GetPresences()
        {
            var Presences = await _presenceService.GetAllPresencesAsync();
            return Ok(Presences);
        }

        [HttpPost]
        [ProducesResponseType(typeof(PresenceResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<PresenceResponse>> CreatePresence([FromBody] CreatePresenceRequest request)
        {
            var Presence = await _presenceService.CreatePresenceAsync(request);
            return CreatedAtAction(nameof(GetPresence), new { id = Presence.Id }, Presence);
        }

        [HttpGet("by_dId_gId")]
        [ProducesResponseType(typeof(PresenceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PresenceResponse>> GetPresence([FromQuery] GetPresenceByDisciplineAndGroupRequest request)
        {
            var Presence = await _presenceService.GetPresencesByDisciplineAndGroupAsync(request);
            if (Presence == null)
                return NotFound(new { disciplineId = request.DisciplineId, groupId = request.GroupId });
            return Ok(Presence);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePresence([FromBody] UpdatePresenceRequest request)
        {
            var success = await _presenceService.UpdatePresenceAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpPut("grade")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdatePresenceGrade([FromQuery] UpdatePresenceGradeRequest request, [FromBody] UpdatePresenceRequest presence)
        {
            var existing = await _dbContext.Presences
                .FirstOrDefaultAsync(p => p.DisciplineId == request.DisciplineId &&
                                         p.StudentId == request.StudentId &&
                                         p.Date == presence.Date);

            if (existing != null)
            {
                existing.IsPresent = presence.IsPresent;
            }
            else
            {
                _dbContext.Presences.Add(new Presence
                {
                    DisciplineId = request.DisciplineId,
                    StudentId = request.StudentId,
                    Date = presence.Date,
                    IsPresent = presence.IsPresent
                });
            }
            //var response = new GradePresenceResponse { ClassId = request.ClassId, IsPresent = presence.IsPresent, Date = presence.Date };  для хаба
            return NoContent();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePresence([FromQuery] DeletePresenceByStudentAndDateRequest request)
        {
            var success = await _presenceService.DeletePresenceByStudentAndDateAsync(request);
            if (!success)
                return NotFound(new { studentId = request.StudentId, date = request.Date});

            return NoContent();
        }
    }
}
