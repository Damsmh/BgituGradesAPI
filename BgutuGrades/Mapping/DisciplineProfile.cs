using AutoMapper;
using BgutuGrades.Models.Discipline;
using Grades.Entities;

namespace BgutuGrades.Mapping
{
    public class DisciplineProfile : Profile
    {
        public DisciplineProfile()
        {
            CreateMap<CreateDisciplineRequest, Discipline>();
            CreateMap<UpdateDisciplineRequest, Discipline>();
            CreateMap<Discipline, DisciplineResponse>();
        }
    }
}
