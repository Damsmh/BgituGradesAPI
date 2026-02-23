using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Mark;

namespace BgituGrades.Mapping
{
    public class MarkProfile : Profile
    {
        public MarkProfile()
        {
            CreateMap<CreateMarkRequest, Mark>();
            CreateMap<UpdateMarkRequest, Mark>();
            CreateMap<UpdateMarkRequest, MarkResponse>();
            CreateMap<UpdateMarkGradeRequest, Mark>();
            CreateMap<Mark, MarkResponse>();
        }
    }
}
