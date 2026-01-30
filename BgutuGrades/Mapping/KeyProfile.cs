using AutoMapper;
using BgutuGrades.Entities;
using BgutuGrades.Models.Key;

namespace BgutuGrades.Mapping
{
    public class KeyProfile : Profile
    {
        public KeyProfile()
        {
            CreateMap<ApiKey, KeyResponse>();
        }
    }
}
