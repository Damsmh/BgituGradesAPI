using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Key;

namespace BgituGrades.Mapping
{
    public class KeyProfile : Profile
    {
        public KeyProfile()
        {
            CreateMap<ApiKey, KeyResponse>();
        }
    }
}
