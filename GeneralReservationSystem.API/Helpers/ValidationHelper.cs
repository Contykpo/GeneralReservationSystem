using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.API.Helpers
{
    public static class ValidationHelper
    {
        public static async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken cancellationToken)
        {
            var result = await validator.ValidateAsync(dto, cancellationToken);
            if (!result.IsValid)
                return new BadRequestObjectResult(result.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));
            return null;
        }
    }
}
