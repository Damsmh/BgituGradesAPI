using FluentValidation;
using BgituGrades.Models.Class;

namespace BgituGrades.Validators
{
    public class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
    {
        public CreateClassRequestValidator()
        {
            RuleFor(x => x.WeekDay)
                .InclusiveBetween(1, 7)
                .WithMessage("День недели должен быть от 1 до 7");

            RuleFor(x => x.Weeknumber)
                .InclusiveBetween(1, 2)
                .WithMessage("Номер недели должен быть 1 или 2");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");
        }
    }

    public class GetClassDateRequestValidator : AbstractValidator<GetClassDateRequest>
    {
        public GetClassDateRequestValidator()
        {
            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");
        }
    }
}
