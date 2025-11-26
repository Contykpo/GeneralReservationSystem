using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Diagnostics;
using System.Data.Common;
using System.Net;
using System.Text.Json;

namespace GeneralReservationSystem.Server.Middleware
{
    public static class GlobalExceptionHandler
    {
        public static async Task HandleAsync(HttpContext context)
        {
            ILogger logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
            Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            string message = "Ha ocurrido un error inesperado.";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            if (exception is ServiceNotFoundException notFoundEx)
            {
                logger.LogWarning(notFoundEx, "Resource not found: {error}", notFoundEx.Message);
                message = notFoundEx.Message;
                statusCode = HttpStatusCode.NotFound;
            }
            else if (exception is ServiceBusinessException businessEx)
            {
                logger.LogWarning(businessEx, "Business rule violation: {error}", businessEx.Message);
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
                        logger.LogError(dbEx, "DbException: {error}", dbEx.Message);
                    }
                    else
                    {
                        logger.LogError(repoEx, "SQL error in repository: {error}", repoEx.Message);
                    }
                }
                else
                {
                    logger.LogError(serviceEx, "Service error: {error}", serviceEx.Message);
                }
                message = serviceEx.Message;
                statusCode = HttpStatusCode.InternalServerError;
            }
            else if (exception != null)
            {
                logger.LogError(exception, "Unhandled exception: {error}", exception.Message);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            var error = new { error = message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
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
