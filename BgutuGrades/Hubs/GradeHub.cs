using BgutuGrades.Data;
using BgutuGrades.Models.Class;
using BgutuGrades.Models.Mark;
using BgutuGrades.Models.Presence;
using BgutuGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Saunter.Attributes;

namespace BgutuGrades.Hubs
{
    [AsyncApi]
    public class GradeHub(IClassService classService, IPresenceService presenceService, IMarkService markService, AppDbContext dbContext) : Hub
    {
        private readonly IClassService _classService = classService;
        private readonly IPresenceService _presenceService = presenceService;
        private readonly IMarkService _markService = markService;

        [Channel("hubs/grade/GetMarkGrade")]
        [Authorize(Policy = "ViewOnly")]
        [PublishOperation(typeof(GetClassDateRequest), Summary = "Запросить оценки по работам", OperationId = nameof(GetMarkGrade))]
        [SubscribeOperation(typeof(IEnumerable<FullGradeMarkResponse>), Summary = "Событие: Получение списка оценок (ответ на GetMarkGrade)", OperationId = "ReceiveMarks")]
        public async Task GetMarkGrade(GetClassDateRequest request)
        {
            //string groupName = $"{request.GroupId}_{request.DisciplineId}_mark";
            //await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var marks = await _classService.GetMarksByWorksAsync(request);
            await Clients.Caller.SendAsync("ReceiveMarks", marks);
        }

        [Channel("hubs/grade/GetPresenceGrade")]
        [Authorize(Policy = "ViewOnly")]
        [PublishOperation(typeof(GetClassDateRequest), Summary = "Запросить данные о посещаемости", OperationId = nameof(GetPresenceGrade))]
        [SubscribeOperation(typeof(IEnumerable<FullGradePresenceResponse>), Summary = "Событие: Получение данных о посещаемости (ответ на GetPresenceGrade)", OperationId = "ReceivePresences")]
        public async Task GetPresenceGrade(GetClassDateRequest request)
        {
            //string groupName = $"{request.GroupId}_{request.DisciplineId}_pres";
            //await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var classDates = await _classService.GetPresenceByScheduleAsync(request);
            await Clients.Caller.SendAsync("ReceivePresences", classDates);
        }

        [Channel("hubs/grade/UpdateMarkGrade")]
        [Authorize(Policy = "Edit")]
        [PublishOperation(typeof(UpdateMarkGradeRequest), Summary = "Обновить или создать оценку", OperationId = nameof(UpdateMarkGrade))]
        [SubscribeOperation(typeof(FullGradeMarkResponse), Summary = "Событие: Оценка обновлена (рассылается всем)", OperationId = "UpdatedMark")]
        public async Task UpdateMarkGrade(UpdateMarkGradeRequest request)
        {
            var response = await _markService.UpdateOrCreateMarkAsync(request);
            await Clients.All.SendAsync("UpdatedMark", response);
        }

        [Channel("hubs/grade/UpdatePresenceGrade")]
        [Authorize(Policy = "Edit")]
        [PublishOperation(typeof(UpdatePresenceGradeRequest), Summary = "Обновить или создать запись о посещаемости", OperationId = nameof(UpdatePresenceGrade))]
        [SubscribeOperation(typeof(FullGradePresenceResponse), Summary = "Событие: Посещаемость обновлена (рассылается всем)", OperationId = "UpdatedPresence")]
        public async Task UpdatePresenceGrade(int classId, UpdatePresenceGradeRequest request)
        {
            var response = await _presenceService.UpdateOrCreatePresenceAsync(classId, request);
            await Clients.All.SendAsync("UpdatedPresence", response);
        }
    }
}
