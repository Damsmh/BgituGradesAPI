using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Student;

namespace BgituGrades.Mapping
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            CreateMap<CreateStudentRequest, Student>();
            CreateMap<UpdateStudentRequest, Student>();
            CreateMap<Student, StudentResponse>();
        }
    }
}
