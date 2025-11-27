using GeneralReservationSystem.Application.Exceptions.Services;
using System.Net;
using System.Text.Json;

namespace GeneralReservationSystem.Server.Middleware
{
    public class ApiServiceExceptionHandler(RequestDelegate next, ILogger<ApiServiceExceptionHandler> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (ServiceException ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, ServiceException exception)
        {
            string message;
            HttpStatusCode statusCode;
            object? responseBody = null;

            switch (exception)
            {
                case ServiceValidationException validationEx:
                    message = validationEx.Message;
                    statusCode = HttpStatusCode.BadRequest;
                    responseBody = new { errorMessage = message, errors = validationEx.Errors };
                    break;

                case ServiceNotFoundException notFoundEx:
                    message = notFoundEx.Message;
                    statusCode = HttpStatusCode.NotFound;
                    break;

                case ServiceDuplicateException duplicateEx:
                    message = duplicateEx.Message;
                    statusCode = HttpStatusCode.Conflict;
                    break;

                case ServiceReferenceException referenceEx:
                    message = referenceEx.Message;
                    statusCode = HttpStatusCode.Conflict;
                    break;

                case ServiceBusinessException businessEx:
                    message = businessEx.Message;
                    statusCode = HttpStatusCode.BadRequest;
                    break;

                default:
                    message = exception.Message;
                    statusCode = HttpStatusCode.InternalServerError;
                    logger.LogError(exception, "Internal server error occurred: {Message}", message);
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            responseBody ??= new { error = message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
        }
    }
}
