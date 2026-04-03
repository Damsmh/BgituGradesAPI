using BgituGrades.Models.Student;
using FluentValidation;

namespace BgituGrades.Validators
{
    public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
    {
        public CreateStudentRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Имя студента не может быть пустым")
                .MaximumLength(255)
                    .WithMessage("Имя студента не может быть длиннее 255 символов");

            RuleFor(x => x.GroupId)
                .GreaterThan(0)
                    .WithMessage("GroupId должен быть больше 0");
            RuleFor(x => x.OfficialId)
                .GreaterThan(0)
                    .WithMessage("OfficialId должен быть больше 0");
        }
    }

    public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
    {
        public UpdateStudentRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                    .WithMessage("id студента должен быть больше 0");

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Имя студента не может быть пустым")
                .MaximumLength(255)
                    .WithMessage("Имя студента не может быть длиннее 255 символов");

            RuleFor(x => x.GroupId)
                .GreaterThan(0)
                    .WithMessage("GroupId должен быть больше 0");
        }
    }
    public class GetStudentsByGroupRequestValidator : AbstractValidator<GetStudentsByGroupRequest>
    {
        public GetStudentsByGroupRequestValidator()
        {
            RuleFor(x => x.GroupIds)
                .NotEmpty()
                    .WithMessage("GroupIds не может быть пустым");
        }
    }
}

