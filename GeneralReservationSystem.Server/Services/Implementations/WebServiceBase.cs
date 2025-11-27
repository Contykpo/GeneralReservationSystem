using FluentValidation;
using FluentValidation.Results;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Security.Claims;
using static GeneralReservationSystem.Application.Constants;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public abstract class WebServiceBase(IHttpContextAccessor httpContextAccessor)
    {
        protected ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        protected int? CurrentUserId => int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : null;

        protected bool IsAdmin => User?.IsInRole(AdminRoleName) ?? false;

        protected bool IsOwnerOrAdmin(int targetUserId)
        {
            return IsAdmin || CurrentUserId == targetUserId;
        }

        protected void EnsureAuthenticated()
        {
            if (CurrentUserId == null)
            {
                throw new ServiceException("No está autorizado para realizar esta acción.");
            }
        }

        protected void EnsureAdmin()
        {
            if (!IsAdmin)
            {
                throw new ServiceException("No tiene permisos para realizar esta acción.");
            }
        }

        protected void EnsureOwnerOrAdmin(int targetUserId)
        {
            if (!IsOwnerOrAdmin(targetUserId))
            {
                throw new ServiceException("No tiene permisos para realizar esta acción.");
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
