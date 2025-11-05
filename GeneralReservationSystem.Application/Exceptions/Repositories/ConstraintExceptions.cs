namespace GeneralReservationSystem.Application.Exceptions.Repositories
{
    // NOTA DE IMPLEMENTACIÓN: Estas excepciones están diseñadas para ser lanzadas por la capa de repositorios y
    // capturadas en la capa de servicios. Además, podrías argumentar que se están utilizando para el control de
    // flujo (lo cual no es ideal) y tendrías razón, pero estas excepciones surgen de excepciones de la base de
    // datos, por lo que son excepcionales por naturaleza. Además, llevan información útil sobre la restricción
    // violada que se perdería si solo devolviéramos un booleano o un enum. Son excepciones que forman parte del
    // funcionamiento normal de la aplicación, pero aún representan situaciones excepcionales que deben ser manejadas
    // explícitamente.
    // Alternativamente, podríamos devolver un tipo Result<T>, pero eso complicaría la interfaz del repositorio y
    // las implementaciones de los servicios, y requeriría más código repetitivo para manejar los resultados. Además,
    // eso requeriría capturar las excepciones de la base de datos en la capa de repositorio, lo que la haría menos
    // transparente. Aún peor si necesitamos traducir los mensajes de error a diferentes idiomas: rompería
    // responsabilidades, porque la capa de repositorio tendría que conocer sobre localización, lo cual no es
    // su responsabilidad. Utilizando excepciones específicas, la capa de servicios puede decidir cómo manejar cada
    // caso fácilmente, manteniendo la capa de repositorio simple y enfocada en la interacción con la base de datos.
    // Con respeto a la localización, utilizando excepciones específicas la capa de servicios solo debe generar el
    // mensaje de error localizado una vez, en vez de traducir mensajes de error genéricos (en formato de string)
    // que serían devueltos en un Result<T> de error (se podría usar un enum quizás, pero sería complicar demasiado
    // el código).
    public abstract class RepositoryConstraintException : RepositoryException
    {
        protected RepositoryConstraintException(string message) : base(message) { }
        protected RepositoryConstraintException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class PrimaryKeyViolationException : RepositoryConstraintException
    {
        public string ConstraintName { get; }

        public PrimaryKeyViolationException(string constraintName)
            : base($"Primary key constraint '{constraintName}' violated.")
        {
            ConstraintName = constraintName;
        }
        public PrimaryKeyViolationException(string constraintName, Exception innerException)
            : base($"Primary key constraint '{constraintName}' violated.", innerException)
        {
            ConstraintName = constraintName;
        }
    }

    public class UniqueConstraintViolationException : RepositoryConstraintException
    {
        public string ConstraintName { get; }

        public UniqueConstraintViolationException(string constraintName)
            : base($"Unique constraint '{constraintName}' violated.")
        {
            ConstraintName = constraintName;
        }
        public UniqueConstraintViolationException(string constraintName, Exception innerException)
            : base($"Unique constraint '{constraintName}' violated.", innerException)
        {
            ConstraintName = constraintName;
        }
    }

    public class ForeignKeyViolationException : RepositoryConstraintException
    {
        public string ConstraintName { get; }

        public ForeignKeyViolationException(string constraintName)
            : base($"Foreign key constraint '{constraintName}' violated.")
        {
            ConstraintName = constraintName;
        }
        public ForeignKeyViolationException(string constraintName, Exception innerException)
            : base($"Foreign key constraint '{constraintName}' violated.", innerException)
        {
            ConstraintName = constraintName;
        }
    }

    public class CheckConstraintViolationException : RepositoryConstraintException
    {
        public string ConstraintName { get; }

        public CheckConstraintViolationException(string constraintName)
            : base($"Check constraint '{constraintName}' violated.")
        {
            ConstraintName = constraintName;
        }
        public CheckConstraintViolationException(string constraintName, Exception innerException)
            : base($"Check constraint '{constraintName}' violated.", innerException)
        {
            ConstraintName = constraintName;
        }
    }

    public class NotNullConstraintViolationException : RepositoryConstraintException
    {
        public string ColumnName { get; }

        public NotNullConstraintViolationException(string columnName)
            : base($"Not-null constraint on column '{columnName}' violated.")
        {
            ColumnName = columnName;
        }
        public NotNullConstraintViolationException(string columnName, Exception innerException)
            : base($"Not-null constraint on column '{columnName}' violated.", innerException)
        {
            ColumnName = columnName;
        }
    }
}
