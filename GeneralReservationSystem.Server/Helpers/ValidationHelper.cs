using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.Server.Helpers
{
    public static class ValidationHelper
    {
        public static async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken cancellationToken)
        {
            FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(dto, cancellationToken);
            return !result.IsValid
                ? new BadRequestObjectResult(result.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }))
                : null;
        }
    }
}
