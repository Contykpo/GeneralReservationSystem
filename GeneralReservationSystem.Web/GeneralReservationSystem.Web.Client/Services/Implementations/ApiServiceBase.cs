using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public abstract class ApiServiceBase(HttpClient httpClient)
    {
        protected readonly HttpClient _httpClient = httpClient;
        protected static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static HttpRequestMessage CreateRequestWithCredentials(HttpMethod method, string url)
        {
            HttpRequestMessage request = new(method, url);
            _ = request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return request;
        }

        protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Get, url);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task<T> PostAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task PostAsync(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task<T> PutAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Put, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Delete, url);
            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string errorContent = await response.Content.ReadAsStringAsync();

            throw response.StatusCode switch
            {
                HttpStatusCode.BadRequest when CheckBadRequestForValidationErrors(errorContent) =>
                    new ServiceValidationException($"La solicitud es inválida.", ParseBadRequestErrors(errorContent)),
                HttpStatusCode.Unauthorized => new ServiceBusinessException($"No está autorizado para realizar esta acción."),
                HttpStatusCode.Forbidden => new ServiceBusinessException($"No tiene permisos para realizar esta acción."),
                HttpStatusCode.NotFound => new ServiceNotFoundException(ParseErrorMessage(errorContent)),
                HttpStatusCode.Conflict => new ServiceBusinessException(ParseErrorMessage(errorContent)),
                _ => new ServiceException(ParseErrorMessage(errorContent))
            };
        }

        private static bool CheckBadRequestForValidationErrors(string errorObj)
        {
            return !string.IsNullOrWhiteSpace(errorObj); // Assuming any content indicates validation errors.
        }

        private class ErrorResponse
        {
            public string? Error { get; set; }
        }

        private static string ParseErrorMessage(string errorContents)
        {

            try
            {
                ErrorResponse? errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContents, jsonOptions);
                if (errorResponse?.Error != null)
                {
                    return errorResponse.Error;
                }
            }
            catch
            {
            }
            return "Error en la solicitud al servidor.";
        }

        private static ValidationError[] ParseBadRequestErrors(string errorObj)
        {
            try
            {
                ValidationError[]? errors = JsonSerializer.Deserialize<ValidationError[]>(errorObj, jsonOptions);
                if (errors != null)
                {
                    return errors;
                }
            }
            catch
            {
            }

            try
            {
                ValidationError? error = JsonSerializer.Deserialize<ValidationError>(errorObj, jsonOptions);
                if (error != null)
                {
                    return [error];
                }
            }
            catch
            {
            }

            return [];
        }
    }
}
