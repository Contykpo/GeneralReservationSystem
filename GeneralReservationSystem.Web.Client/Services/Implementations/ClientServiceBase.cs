using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public abstract class ClientServiceBase(HttpClient httpClient)
    {
        protected static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected HttpClient HttpClient { get; } = httpClient;

        protected static HttpRequestMessage CreateRequestWithCredentials(HttpMethod method, string url)
        {
            HttpRequestMessage request = new(method, url);
            _ = request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return request;
        }

        protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Get, url);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task<T> PostAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task PostAsync(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task PutAsync(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Put, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task PatchAsync(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Patch, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task<T> PutAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Put, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task<T> PatchAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Patch, url);
            request.Content = JsonContent.Create(content);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Delete, url);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task<T> PostMultipartAsync<T>(string url, MultipartFormDataContent content, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = content;
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task<(byte[] FileContent, string FileName)> GetFileAsync(string url, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = CreateRequestWithCredentials(HttpMethod.Get, url);
            HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);

            byte[] fileContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            string fileName = ExtractFileName(response);

            return (fileContent, fileName);
        }

        private static string ExtractFileName(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentDisposition?.FileName != null)
            {
                string fileName = response.Content.Headers.ContentDisposition.FileName;
                return fileName.Trim('"', '\'');
            }

            if (response.Content.Headers.TryGetValues("Content-Disposition", out IEnumerable<string>? values))
            {
                string? contentDisposition = values.FirstOrDefault();
                if (!string.IsNullOrEmpty(contentDisposition))
                {
                    Match match = Regex.Match(contentDisposition, @"filename[*]?=[""']?([^""';]+)[""']?", RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }

            return "download";
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
                HttpStatusCode.BadRequest when TryParseValidationErrorResponse(errorContent, out ValidationErrorResponse? errorResponse) =>
                    new ServiceValidationException(errorResponse!.ErrorMessage, errorResponse!.Errors),
                HttpStatusCode.BadRequest => new ServiceBusinessException(ParseErrorMessage(errorContent) ?? "Error en la solicitud."),
                HttpStatusCode.Unauthorized => new ServiceException(ParseErrorMessage(errorContent) ?? "No está autorizado para realizar esta acción."),
                HttpStatusCode.Forbidden => new ServiceException(ParseErrorMessage(errorContent) ?? "No tiene permisos para realizar esta acción."),
                HttpStatusCode.NotFound => new ServiceNotFoundException(ParseErrorMessage(errorContent) ?? "No se encontró el recurso solicitado."),
                HttpStatusCode.Conflict => new ServiceBusinessException(ParseErrorMessage(errorContent) ?? "Conflicto en la solicitud."),
                _ => new ServiceException(ParseErrorMessage(errorContent) ?? "Error desconocido.")
            };
        }

        private class ErrorResponse
        {
            public required string Error { get; set; }
        }

        private class ValidationErrorResponse
        {
            public required string ErrorMessage { get; set; }
            public required ValidationError[] Errors { get; set; }
        }

        private static string? ParseErrorMessage(string errorContents)
        {
            try
            {
                ErrorResponse? errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContents, jsonOptions);
                return errorResponse?.Error;
            }
            catch
            {
            }

            return null;
        }

        private static bool TryParseValidationErrorResponse(string errorContents, out ValidationErrorResponse? errorResponse)
        {
            errorResponse = null;

            try
            {
                errorResponse = JsonSerializer.Deserialize<ValidationErrorResponse>(errorContents, jsonOptions);
                if (errorResponse != null)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
