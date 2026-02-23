using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Class;

namespace BgituGrades.Mapping
{
    public class ClassProfile : Profile
    {
        public ClassProfile()
        {
            CreateMap<CreateClassRequest, Class>();
            CreateMap<GetClassDateRequest, ClassDateResponse>();
            CreateMap<Class, ClassResponse>();
        }
    }
}
