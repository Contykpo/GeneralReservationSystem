using GeneralReservationSystem.Application.Exceptions.Services;

namespace GeneralReservationSystem.Web.Exceptions
{
    public class ServiceValidationException(string message, ValidationError[] errors) : ServiceBusinessException(message)
    {
        public ValidationError[] Errors { get; } = errors;
    }
}