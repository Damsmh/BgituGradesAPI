using AutoMapper;
using BgutuGrades.Models.Work;
using Grades.Entities;

namespace BgutuGrades.Mapping
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
