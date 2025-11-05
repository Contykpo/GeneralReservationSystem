namespace GeneralReservationSystem.Application.Exceptions.Repositories
{
    // NOTA DE IMPLEMENTACIÓN: Estas excepciones están diseñadas para ser lanzadas por la capa de repositorios y
    // capturadas en la capa de servicios. Abstraen errores comunes relacionados con el acceso a datos, como
    // indisponibilidad del repositorio, tiempos de espera y problemas de concurrencia. Al utilizar excepciones
    // específicas, la capa de servicios puede manejar cada caso de manera adecuada, manteniendo la capa de
    // repositorio simple y enfocada en la interacción con la base de datos.
    public class RepositoryException : Exception
    {
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RepositoryUnavailableException : RepositoryException
    {
        public RepositoryUnavailableException(string message) : base(message) { }
        public RepositoryUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RepositoryTimeoutException : RepositoryException
    {
        public RepositoryTimeoutException(string message) : base(message) { }
        public RepositoryTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RepositoryConcurrencyException : RepositoryException
    {
        public RepositoryConcurrencyException(string message) : base(message) { }
        public RepositoryConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
    }
}
