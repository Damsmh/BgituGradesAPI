using BgituGrades.Models.Work;
using FluentValidation;

namespace BgituGrades.Validators
{
    public class CreateWorkRequestValidator : AbstractValidator<CreateWorkRequest>
    {
        public CreateWorkRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя работы не может быть пустым")
                .MaximumLength(255).WithMessage("Имя работы не может быть длиннее 255 символов");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.IssuedDate)
                .NotEmpty().WithMessage("Дата выдачи не может быть пустой");
        }
    }

    public class UpdateWorkRequestValidator : AbstractValidator<UpdateWorkRequest>
    {
        public UpdateWorkRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id работы должен быть больше 0");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя работы не может быть пустым")
                .MaximumLength(255).WithMessage("Имя работы не может быть длиннее 255 символов");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.IssuedDate)
                .NotEmpty().WithMessage("Дата выдачи не может быть пустой");
        }
    }
}
