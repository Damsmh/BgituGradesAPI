using AutoMapper;
using BgutuGrades.Data;
using BgutuGrades.Models.Class;
using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;
using BgutuGrades.Services;
using Grades.Entities;
using Microsoft.AspNetCore.Mvc;
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
            var works = await _classService.GetMarksByWorksAsync(request);
            await Clients.Caller.SendAsync("Receive", works);
        }

        public async Task GetPresenceGrade(GetClassDateRequest request)
        {
            var classDates = await _classService.GetPresenceByScheduleAsync(request);
            await Clients.Caller.SendAsync("Receive", classDates);
        }

        public async Task UpdateMarkGrade([FromQuery] UpdateMarkGradeRequest request, [FromBody] UpdateMarkRequest mark)
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
                await _dbContext.Marks.AddAsync(new Mark
                {
                    StudentId = request.StudentId,
                    WorkId = mark.WorkId,
                    Value = mark.Value,
                    Date = mark.Date,
                    IsOverdue = mark.IsOverdue
                });
            }

            await _dbContext.SaveChangesAsync();

            await Clients.All.SendAsync("Updated", new FullGradeMarkResponse
            {
                StudentId = request.StudentId,
                Marks = [new GradeMarkResponse
                {
                    WorkId = mark.WorkId,
                    Value = mark.Value,
                }]
            });
        }

        public async Task UpdatePresenceGrade([FromQuery] UpdatePresenceGradeRequest request, [FromBody] UpdatePresenceRequest presence)
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
                await _dbContext.Presences.AddAsync(new Presence
                {
                    DisciplineId = request.DisciplineId,
                    StudentId = request.StudentId,
                    Date = presence.Date,
                    IsPresent = presence.IsPresent
                });
            }
            var response = new FullGradePresenceResponse {
                StudentId = request.StudentId,
                Presences = [new GradePresenceResponse { 
                    ClassId = request.ClassId, 
                    IsPresent = presence.IsPresent, 
                    Date = presence.Date 
                }] 
            };
            await Clients.All.SendAsync("Updated", response);
        }
    }
}
