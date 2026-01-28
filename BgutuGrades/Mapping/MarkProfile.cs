using AutoMapper;
using BgutuGrades.Models.Mark;
using Grades.Entities;

namespace BgutuGrades.Mapping
{
    public class MarkProfile : Profile
    {
        public MarkProfile()
        {
            CreateMap<CreateMarkRequest, Mark>();
            CreateMap<UpdateMarkRequest, Mark>();
            CreateMap<Mark, MarkResponse>();
        }
    }
}
