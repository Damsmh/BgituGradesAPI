using AutoMapper;
using BgutuGrades.Models.Presence;
using Grades.Entities;

namespace BgutuGrades.Mapping
{
    public class PresenceProfile : Profile
    {
        public PresenceProfile()
        {
            CreateMap<CreatePresenceRequest, Presence>();
            CreateMap<UpdatePresenceGradeRequest, Presence>();
            CreateMap<Presence, PresenceResponse>();
        }
    }
}
