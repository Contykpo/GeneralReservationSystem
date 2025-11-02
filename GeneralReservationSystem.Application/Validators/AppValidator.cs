using FluentValidation;
using FluentValidation.Results;

namespace GeneralReservationSystem.Application.Validators
{
    public abstract class AppValidator<T> : AbstractValidator<T>
    {
        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            ValidationResult result = await ValidateAsync(ValidationContext<T>.CreateWithOptions((T)model, x => x.IncludeProperties(propertyName)));
            return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
