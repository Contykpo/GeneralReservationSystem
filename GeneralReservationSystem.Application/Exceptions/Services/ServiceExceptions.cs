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

    public class ServiceNotFoundException : ServiceException
    {
        public ServiceNotFoundException(string message) : base(message) { }
        public ServiceNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
