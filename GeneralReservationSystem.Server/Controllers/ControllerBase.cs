using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Security;
using System.Security.Claims;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Controllers
{
    public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected bool IsAdmin => User?.IsInRole(AdminRoleName) ?? false;

        protected int CurrentUserId
        {
            get
            {
                if (!(User?.Identity?.IsAuthenticated ?? false))
                {
                    throw new UnauthorizedAccessException("No está autorizado para realizar esta acción.");
                }

                string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return !int.TryParse(userIdStr, out int id)
                    ? throw new UnauthorizedAccessException("No está autorizado para realizar esta acción.")
                    : id;
            }
        }

        protected void EnsureOwnerOrAdmin(int targetUserId)
        {
            if (!IsAdmin && CurrentUserId != targetUserId)
            {
                throw new SecurityException("No tiene permisos para realizar esta acción.");
            }
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
