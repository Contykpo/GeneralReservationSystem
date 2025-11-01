using GeneralReservationSystem.Application.Exceptions.Repositories;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace GeneralReservationSystem.Infrastructure.Helpers
{
    public enum SqlConstraintViolationType
    {
        PrimaryKey,
        Unique,
        ForeignKey,
        Check,
        NotNull
    }

    public static class SqlExceptionHelper
    {
        public static SqlConstraintViolationType? DetermineViolationType(string sqlErrorMessage)
        {
            if (string.IsNullOrEmpty(sqlErrorMessage))
            {
                return null;
            }

            Match match = RegexHelpers.ConstraintTypeRegex().Match(sqlErrorMessage);
            if (!match.Success)
            {
                return null;
            }

            if (match.Groups["PrimaryKey"].Success)
            {
                return SqlConstraintViolationType.PrimaryKey;
            }

            if (match.Groups["Unique"].Success)
            {
                return SqlConstraintViolationType.Unique;
            }

            if (match.Groups["ForeignKey"].Success)
            {
                return SqlConstraintViolationType.ForeignKey;
            }

            if (match.Groups["Check"].Success)
            {
                return SqlConstraintViolationType.Check;
            }

            if (match.Groups["NotNull"].Success)
            {
                return SqlConstraintViolationType.NotNull;
            }

            return null; // Maybe not a constraint violation.
        }

        public static string? ExtractConstraintName(string sqlErrorMessage)
        {
            if (string.IsNullOrEmpty(sqlErrorMessage))
            {
                return null;
            }

            Match typeMatch = RegexHelpers.ConstraintTypeRegex().Match(sqlErrorMessage);
            if (typeMatch.Success)
            {
                if (typeMatch.Groups["name"].Success && !string.IsNullOrEmpty(typeMatch.Groups["name"].Value))
                {
                    return typeMatch.Groups["name"].Value;
                }
            }

            Match match = RegexHelpers.ConstraintNameRegex().Match(sqlErrorMessage);
            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            return null; // Constraint name not found.
        }

        public static string? ExtractNotNullColumnName(string sqlErrorMessage)
        {
            if (string.IsNullOrEmpty(sqlErrorMessage))
            {
                return null;
            }

            Match typeMatch = RegexHelpers.ConstraintTypeRegex().Match(sqlErrorMessage);
            if (typeMatch.Success && typeMatch.Groups["columnName"].Success)
            {
                return typeMatch.Groups["columnName"].Value;
            }

            Match match = RegexHelpers.NotNullColumnRegex().Match(sqlErrorMessage);
            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            return null; // Does not apply.
        }

        // IMPLEMENTATION NOTE: You may think it's better to just throw a new exception here, but that
        // breaks the debugger if this function is called inside a catch block (which is what should happen).
        // It's a known issue with .NET: if a new exception is thrown in a static method inside a catch block,
        // the debugger cannot step into that method. Returning the exception instead allows the caller to throw it,
        // preserving the stack trace and allowing the debugger to function correctly. This only happens on debug,
        // and this is a workaround to keep the debugging experience smooth.
        // It doesn't matter that the exceptions are masked behind a Exception type, because if they are thrown,
        // they will be caught as their actual type.
        public static RepositoryConstraintException? GetConstraintViolationException(DbException ex)
        {
            SqlConstraintViolationType? violationType = DetermineViolationType(ex.Message);
            if (violationType == null)
            {
                return null; // Not a recognized constraint violation.  
            }

            return violationType switch
            {
                SqlConstraintViolationType.PrimaryKey => new PrimaryKeyViolationException(ExtractConstraintName(ex.Message) ?? "Unknown", ex),
                SqlConstraintViolationType.Unique => new UniqueConstraintViolationException(ExtractConstraintName(ex.Message) ?? "Unknown", ex),
                SqlConstraintViolationType.ForeignKey => new ForeignKeyViolationException(ExtractConstraintName(ex.Message) ?? "Unknown", ex),
                SqlConstraintViolationType.Check => new CheckConstraintViolationException(ExtractConstraintName(ex.Message) ?? "Unknown", ex),
                SqlConstraintViolationType.NotNull => new NotNullConstraintViolationException(ExtractNotNullColumnName(ex.Message) ?? "Unknown", ex),
                _ => null,// Not a recognized constraint violation.
            };
        }

        public static RepositoryException ToRepositoryException(DbException ex)
        {
            RepositoryConstraintException? constraintException = GetConstraintViolationException(ex);
            if (constraintException != null)
            {
                return constraintException;
            }

            // 57014: PostgreSQL timeout error code
            if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("57014", StringComparison.OrdinalIgnoreCase))
            {
                return new RepositoryTimeoutException("The repository operation timed out.", ex);
            }

            // 40P01: PostgreSQL deadlock error code
            if (ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("40P01", StringComparison.OrdinalIgnoreCase))
            {
                return new RepositoryConcurrencyException("A concurrency conflict occurred in the repository.", ex);
            }

            // 08000: Connection exception, 08003: Connection does not exist, 08006: Connection failure
            if (ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("08000", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("08003", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("08006", StringComparison.OrdinalIgnoreCase))
            {
                return new RepositoryUnavailableException("The repository is unavailable.", ex);
            }

            // 40001: PostgreSQL serialization failure error code
            if (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("serialization", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("40001", StringComparison.OrdinalIgnoreCase))
            {
                return new RepositoryConcurrencyException("A concurrency conflict occurred in the repository.", ex);
            }

            return new RepositoryException("The repository operation failed.", ex);
        }
    }
}
