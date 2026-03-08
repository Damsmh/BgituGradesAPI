using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Setting;

namespace BgituGrades.Mapping
{
    public class SettingProfile : Profile
    {
        public SettingProfile()
        {
            CreateMap<UpdateSettingRequest, Setting>();
            CreateMap<Setting, SettingResponse>();
        }
    }
}
