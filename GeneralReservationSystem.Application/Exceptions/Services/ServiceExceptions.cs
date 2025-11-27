namespace GeneralReservationSystem.Application.Exceptions.Services
{
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message) { }
        public ServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceBusinessException : ServiceException
    {
        public ServiceBusinessException(string message) : base(message) { }
        public ServiceBusinessException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceDuplicateException : ServiceBusinessException
    {
        public ServiceDuplicateException(string message) : base(message) { }
        public ServiceDuplicateException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceReferenceException : ServiceBusinessException
    {
        public ServiceReferenceException(string message) : base(message) { }
        public ServiceReferenceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceNotFoundException : ServiceException
    {
        public ServiceNotFoundException(string message) : base(message) { }
        public ServiceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ServiceValidationException(string message, ValidationError[] errors) : ServiceException(message)
    {
        public ValidationError[] Errors { get; } = errors;
    }

    public sealed record ValidationError(string Error, string? Field);
}
