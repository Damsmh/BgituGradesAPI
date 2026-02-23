using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Group;
using BgituGrades.DTO;

namespace BgituGrades.Mapping
{
    public class GroupProfile : Profile
    {
        public GroupProfile()
        {
            CreateMap<CreateGroupRequest, Group>();
            CreateMap<UpdateGroupRequest, Group>();
            CreateMap<Group, GroupResponse>();
        }
    }
}
