using BgutuGrades.Data;
using BgutuGrades.Models.Class;
using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;
using BgutuGrades.Services;
using Grades.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BgutuGrades.Hubs
{
    public class GradeHub(IClassService classService, AppDbContext dbContext) : Hub
    {
        private readonly IClassService _classService = classService;
        private readonly AppDbContext _dbContext = dbContext;

        public async Task GetMarkGrade(GetClassDateRequest request)
        {
            var marks = await _classService.GetMarksByWorksAsync(request);
            await Clients.Caller.SendAsync("ReceiveMarks", marks);
        }

        public async Task GetPresenceGrade(GetClassDateRequest request)
        {
            var classDates = await _classService.GetPresenceByScheduleAsync(request);
            await Clients.Caller.SendAsync("ReceivePresences", classDates);
        }

        public async Task UpdateMarkGrade(UpdateMarkGradeRequest request)
        {
            var existing = await _dbContext.Marks
                .FirstOrDefaultAsync(m => m.StudentId == request.StudentId && m.WorkId == request.WorkId);

            if (existing != null)
            {
                existing.Value = request.Value;
                existing.Date = request.Date;
                existing.IsOverdue = request.IsOverdue;
            }
            else
            {
                await _dbContext.Marks.AddAsync(new Mark
                {
                    StudentId = request.StudentId,
                    WorkId = request.WorkId,
                    Value = request.Value,
                    Date = request.Date,
                    IsOverdue = request.IsOverdue
                });
            }

            await _dbContext.SaveChangesAsync();

            await Clients.All.SendAsync("UpdatedMark", new FullGradeMarkResponse
            {
                StudentId = request.StudentId,
                Marks = [new GradeMarkResponse
                {
                    WorkId = request.WorkId,
                    Value = request.Value,
                }]
            });
        }

        public async Task UpdatePresenceGrade(UpdatePresenceGradeRequest request)
        {

            var presence = await _dbContext.Presences
                .FirstOrDefaultAsync(p => p.DisciplineId == request.DisciplineId &&
                                         p.StudentId == request.StudentId &&
                                         p.Date == request.Date);

            if (presence != null)
            {
                presence.IsPresent = request.IsPresent;
            }
            else
            {
                presence = new Presence
                {
                    DisciplineId = request.DisciplineId,
                    StudentId = request.StudentId,
                    Date = request.Date,
                    IsPresent = request.IsPresent
                };
                await _dbContext.Presences.AddAsync(presence);
            }
            var response = new FullGradePresenceResponse {
                StudentId = request.StudentId,
                Presences = [new GradePresenceResponse { 
                    ClassId = request.ClassId, 
                    IsPresent = request.IsPresent, 
                    Date = request.Date 
                }] 
            };
            await Clients.All.SendAsync("UpdatedPresence", response);
        }
    }
}
