using BgituGrades.Models.Setting;
using FluentValidation;

namespace BgituGrades.Validators
{
    public class UpdateSettingValidator : AbstractValidator<UpdateSettingRequest>
    {
        public UpdateSettingValidator() {
            RuleFor(x => x.CalendarUrl)
                .NotEmpty().WithMessage("Ссылка на календарный учебный график не должна быть пустая.")
                .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("Ссылка должна быть валидной.");
        }
    }
}
