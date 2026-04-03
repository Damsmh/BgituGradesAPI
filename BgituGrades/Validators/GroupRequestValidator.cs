using BgituGrades.Models.Group;
using FluentValidation;

namespace BgituGrades.Validators
{
    public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Имя группы не может быть пустым")
                .MaximumLength(255)
                    .WithMessage("Имя группы не может быть длиннее 255 символов");

            RuleFor(x => x.StudyStartDate)
                .LessThan(x => x.StudyEndDate)
                    .WithMessage("Дата начала должна быть раньше даты окончания");

            RuleFor(x => x.StartWeekNumber)
                .InclusiveBetween(1, 2)
                .WithMessage("Номер недели начала обучения должен быть 1 или 2");
        }
    }
    public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
    {
        public UpdateGroupRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                    .WithMessage("ID группы должен быть больше 0");

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Имя группы не может быть пустым")
                .MaximumLength(255)
                    .WithMessage("Имя группы не может быть длиннее 255 символов");

            RuleFor(x => x.StudyStartDate)
                .LessThan(x => x.StudyEndDate)
                    .WithMessage("Дата начала должна быть раньше даты окончания");

            RuleFor(x => x.StartWeekNumber)
                .InclusiveBetween(1, 2)
                .WithMessage("Номер недели начала обучения должен быть 1 или 2");
        }
    }

    public class GetGroupsByDisciplineRequestValidator : AbstractValidator<GetGroupsByDisciplineRequest>
    {
        public GetGroupsByDisciplineRequestValidator()
        {
            RuleFor(x => x.DisciplineId)
                .GreaterThan(0)
                    .WithMessage("DisciplineId должен быть больше 0");
        }
    }
}
