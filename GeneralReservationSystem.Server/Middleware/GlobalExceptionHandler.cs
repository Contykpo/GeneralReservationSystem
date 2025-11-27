using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Data.Common;
using System.Net;
using System.Security;
using System.Text.Json;

namespace GeneralReservationSystem.Server.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            string message = "Ha ocurrido un error inesperado";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            object? responseBody = null;

            if (exception is ServiceValidationException validationEx)
            {
                message = validationEx.Message;
                statusCode = HttpStatusCode.BadRequest;
                responseBody = new { errorMessage = message, errors = validationEx.Errors };
            }
            else if (exception is ServiceNotFoundException notFoundEx)
            {
                message = notFoundEx.Message;
                statusCode = HttpStatusCode.NotFound;
            }
            else if (exception is ServiceDuplicateException duplicateEx)
            {
                message = duplicateEx.Message;
                statusCode = HttpStatusCode.Conflict;
            }
            else if (exception is ServiceReferenceException referenceEx)
            {
                message = referenceEx.Message;
                statusCode = HttpStatusCode.Conflict;
            }
            else if (exception is ServiceBusinessException businessEx)
            {
                message = businessEx.Message;
                statusCode = HttpStatusCode.BadRequest;
            }
            else if (exception is ServiceException serviceEx)
            {
                if (serviceEx.InnerException is RepositoryException repoEx)
                {
                    DbException? dbEx = GetInnermostDbException(repoEx);
                    if (dbEx != null)
                    {
                        _logger.LogError(dbEx, "DbException: {error}", dbEx.Message);
                    }
                    else
                    {
                        _logger.LogError(repoEx, "SQL error in repository: {error}", repoEx.Message);
                    }
                }
                else
                {
                    _logger.LogError(serviceEx, "Service error: {error}", serviceEx.Message);
                }
                message = serviceEx.Message;
                statusCode = HttpStatusCode.InternalServerError;
            }
            else if (exception is SecurityException securityEx)
            {
                message = securityEx.Message;
                statusCode = HttpStatusCode.Forbidden;
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception: {error}", exception.Message);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            responseBody ??= new { error = message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
        }

        private static DbException? GetInnermostDbException(Exception ex)
        {
            Exception? innerEx = ex;
            while (innerEx != null)
            {
                if (innerEx is DbException dbEx)
                {
                    return dbEx;
                }
                innerEx = innerEx.InnerException;
            }
            return null;
        }
    }
}
