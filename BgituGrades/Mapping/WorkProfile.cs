using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Work;

namespace BgituGrades.Mapping
{
    public class WorkProfile : Profile
    {
        public WorkProfile()
        {
            CreateMap<CreateWorkRequest, Work>();
            CreateMap<UpdateWorkRequest, Work>();
            CreateMap<Work, WorkResponse>();
        }
    }
}
