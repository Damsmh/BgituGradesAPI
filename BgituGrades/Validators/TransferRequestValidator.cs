using FluentValidation;
using BgituGrades.Models.Transfer;

namespace BgituGrades.Validators
{
    public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
    {
        public CreateTransferRequestValidator()
        {
            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");

            RuleFor(x => x.OriginalDate)
                .NotEmpty().WithMessage("Исходная дата не может быть пустой");

            RuleFor(x => x.NewDate)
                .NotEmpty().WithMessage("Новая дата не может быть пустой")
                .NotEqual(x => x.OriginalDate).WithMessage("Новая дата должна отличаться от исходной");
        }
    }

    public class UpdateTransferRequestValidator : AbstractValidator<UpdateTransferRequest>
    {
        public UpdateTransferRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("id переноса должен быть больше 0");

            RuleFor(x => x.DisciplineId)
                .GreaterThan(0).WithMessage("DisciplineId должен быть больше 0");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("GroupId должен быть больше 0");

            RuleFor(x => x.OriginalDate)
                .NotEmpty().WithMessage("Исходная дата не может быть пустой");

            RuleFor(x => x.NewDate)
                .NotEmpty().WithMessage("Новая дата не может быть пустой")
                .NotEqual(x => x.OriginalDate).WithMessage("Новая дата должна отличаться от исходной");
        }
    }
}
