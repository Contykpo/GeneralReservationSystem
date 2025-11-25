using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public abstract class ApiServiceBase(HttpClient httpClient)
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
            return !string.IsNullOrWhiteSpace(errorObj);
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
