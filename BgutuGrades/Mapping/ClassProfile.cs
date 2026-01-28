using AutoMapper;
using BgutuGrades.Entities;
using BgutuGrades.Models.Class;

namespace BgutuGrades.Mapping
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
