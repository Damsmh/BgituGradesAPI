using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Group;

namespace BgituGrades.Mapping
{
    public class GroupProfile : Profile
    {
        public GroupProfile()
        {
            CreateMap<CreateGroupRequest, Group>();
            CreateMap<UpdateGroupRequest, Group>();
            CreateMap<Group, GroupResponse>();
            CreateMap<Group, GroupDTO>();
            CreateMap<Group, CourseReponse>();
            CreateMap<Group, ArchivedGroupResponse>();
            CreateMap<GroupDTO, Group>();
        }
    }
}
