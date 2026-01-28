using AutoMapper;
using BgutuGrades.DTO;
using BgutuGrades.Models.Group;
using Grades.Entities;

namespace BgutuGrades.Mapping
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
