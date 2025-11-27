using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Security.Claims;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers
{
    public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected int? CurrentUserId => int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : null;

        protected bool IsAdmin => User?.IsInRole(AdminRoleName) ?? false;

        protected bool IsOwnerOrAdmin(int targetUserId)
        {
            return IsAdmin || CurrentUserId == targetUserId;
        }

        protected async Task ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken cancellationToken)
        {
            ValidationResult result = await validator.ValidateAsync(dto, cancellationToken);
            if (!result.IsValid)
            {
                ValidationError[] errors = [.. result.Errors.Select(e => new ValidationError(e.ErrorMessage, e.PropertyName))];
                throw new ServiceValidationException("La solicitud es inválida.", errors);
            }
        }
    }
}
