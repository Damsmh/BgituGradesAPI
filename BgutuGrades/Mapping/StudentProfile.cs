using AutoMapper;
using BgutuGrades.Models.Student;
using Grades.Entities;

namespace BgutuGrades.Mapping
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
