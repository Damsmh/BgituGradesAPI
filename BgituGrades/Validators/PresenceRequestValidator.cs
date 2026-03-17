using FluentValidation;
using BgituGrades.Models.Presence;

namespace BgituGrades.Validators
{
    public class CreatePresenceRequestValidator : AbstractValidator<CreatePresenceRequest>
    {
        public CreatePresenceRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Дата не может быть пустой");
        }
    }

    public class UpdatePresenceRequestValidator : AbstractValidator<UpdatePresenceRequest>
    {
        public UpdatePresenceRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id посещаемости должен быть больше 0");

            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Дата не может быть пустой");
        }
    }

    public class GetPresenceByDisciplineAndGroupRequestValidator : AbstractValidator<GetPresenceByDisciplineAndGroupRequest>
    {
        public GetPresenceByDisciplineAndGroupRequestValidator()
        {
            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");
        }
    }

    public class DeletePresenceByStudentAndDateRequestValidator : AbstractValidator<DeletePresenceByStudentAndDateRequest>
    {
        public DeletePresenceByStudentAndDateRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Дата не может быть пустой");
        }
    }
}
