using BgituGrades.Models.Mark;
using FluentValidation;

namespace BgituGrades.Validators
{
    public class CreateMarkRequestValidator : AbstractValidator<CreateMarkRequest>
    {
        public CreateMarkRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.WorkId)
                .GreaterThan(0).WithMessage("WorkId должен быть больше 0");

            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Оценка не может быть пустой")
                .MaximumLength(1).WithMessage("Оценка не может быть длиннее 1 символа");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Дата не может быть пустой");
        }
    }

    public class UpdateMarkRequestValidator : AbstractValidator<UpdateMarkRequest>
    {
        public UpdateMarkRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id оценки должен быть больше 0");

            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.WorkId)
                .GreaterThan(0).WithMessage("WorkId должен быть больше 0");

            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Оценка не может быть пустой")
                .MaximumLength(1).WithMessage("Оценка не может быть длиннее 1 символа");
        }
    }

    public class GetMarksByDisciplineAndGroupRequestValidator : AbstractValidator<GetMarksByDisciplineAndGroupRequest>
    {
        public GetMarksByDisciplineAndGroupRequestValidator()
        {
            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");
        }
    }

    public class DeleteMarkByStudentAndWorkRequestValidator : AbstractValidator<DeleteMarkByStudentAndWorkRequest>
    {
        public DeleteMarkByStudentAndWorkRequestValidator()
        {
            RuleFor(x => x.StudentId)
                .GreaterThan(0).WithMessage("StudentId должен быть больше 0");

            RuleFor(x => x.WorkId)
                .GreaterThan(0).WithMessage("WorkId должен быть больше 0");
        }
    }
}
