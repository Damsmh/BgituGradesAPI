using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Presence;

namespace BgituGrades.Mapping
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
