using FluentValidation;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Security.Claims;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public abstract class WebServiceBase(IHttpContextAccessor httpContextAccessor)
    {
        protected ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

        protected bool IsAdmin => User?.IsInRole("Admin") ?? false;

        protected int? CurrentUserId
        {
            get
            {
                string? userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);
            }
        }

        protected void EnsureAuthorized()
        {
            if (!IsAdmin)
            {
                throw new ServiceBusinessException("No tiene permisos para realizar esta acción.");
            }
        }

        protected void EnsureAuthenticated()
        {
            if (CurrentUserId == null)
            {
                throw new ServiceBusinessException("No está autorizado para realizar esta acción.");
            }
        }

        protected void EnsureOwnerOrAdmin(int targetUserId)
        {
            EnsureAuthenticated();

            if (!IsAdmin && CurrentUserId != targetUserId)
            {
                throw new ServiceBusinessException("No tiene permisos para realizar esta acción.");
            }
        }

        protected async Task ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken cancellationToken)
        {
            FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(dto, cancellationToken);
            if (!result.IsValid)
            {
                ValidationError[] errors = result.Errors
                    .Select(e => new ValidationError(e.ErrorMessage, e.PropertyName))
                    .ToArray();
                throw new ServiceValidationException("La solicitud es inválida.", errors);
            }
        }
    }
}
