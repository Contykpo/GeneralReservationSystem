using GeneralReservationSystem.Application.Exceptions.Repositories;
using GeneralReservationSystem.Application.Exceptions.Services;
using System.Data.Common;
using System.Net;
using System.Text.Json;

namespace GeneralReservationSystem.API.Middleware
{
    public class ExceptionsMiddleware(RequestDelegate next, ILogger<ExceptionsMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (ServiceNotFoundException notFoundEx)
            {
                logger.LogWarning(notFoundEx, "Resource not found: {Message}", notFoundEx.Message);
                await WriteErrorResponse(context, notFoundEx.Message, HttpStatusCode.NotFound);
            }
            catch (ServiceBusinessException businessEx)
            {
                logger.LogWarning(businessEx, "Business rule violation: {Message}", businessEx.Message);
                await WriteErrorResponse(context, businessEx.Message, HttpStatusCode.BadRequest);
            }
            catch (ServiceException serviceEx)
            {
                if (serviceEx.InnerException is RepositoryException repoEx)
                {
                    DbException? dbEx = GetInnermostDbException(repoEx);
                    if (dbEx != null)
                    {
                        logger.LogError(dbEx, "DbException: {Message}", dbEx.Message);
                    }
                    else
                    {
                        logger.LogError(repoEx, "SQL error in repository: {Message}", repoEx.Message);
                    }
                }
                else
                {
                    logger.LogError(serviceEx, "Service error: {Message}", serviceEx.Message);
                }
                await WriteErrorResponse(context, serviceEx.Message, HttpStatusCode.InternalServerError);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await WriteErrorResponse(context, "Ha ocurrido un error inesperado.", HttpStatusCode.InternalServerError);
            }
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

        private static async Task WriteErrorResponse(HttpContext context, string message, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            var error = new { error = message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
