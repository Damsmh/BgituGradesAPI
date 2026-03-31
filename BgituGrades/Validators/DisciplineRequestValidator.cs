using FluentValidation;
using BgituGrades.Models.Discipline;

namespace BgituGrades.Validators
{
    public class CreateDisciplineRequestValidator : AbstractValidator<CreateDisciplineRequest>
    {
        public CreateDisciplineRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя дисциплины не может быть пустым")
                .MaximumLength(255).WithMessage("Имя дисциплины не может быть длиннее 255 символов");
        }
    }

    public class GetDisciplineRequestValidator : AbstractValidator<GetDisciplineByGroupIdsRequest>
    {
        public GetDisciplineRequestValidator()
        {
            RuleFor(x => x.GroupIds)
                .NotEmpty().WithMessage("groupIds не может быть пустым");
        }
    }

    public class UpdateDisciplineRequestValidator : AbstractValidator<UpdateDisciplineRequest>
    {
        public UpdateDisciplineRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id дисциплины должно быть больше 0");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Имя дисциплины не может быть пустым")
                .MaximumLength(255).WithMessage("Имя дисциплины не может быть длиннее 255 символов");
        }
    }

    public class DeleteDisciplineRequestValidator : AbstractValidator<DeleteDisciplineRequest>
    {
        public DeleteDisciplineRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id дисциплины должно быть больше 0");
        }
    }
}
