using GeneralReservationSystem.Application.Exceptions.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeneralReservationSystem.Web.Services.Implementations
{
    public abstract class ApiServiceBase
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;

        protected ApiServiceBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static HttpRequestMessage CreateRequestWithCredentials(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return request;
        }

        protected async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            var request = CreateRequestWithCredentials(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task<T> PostAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            var request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task PostAsync(string url, object content, CancellationToken cancellationToken = default)
        {
            var request = CreateRequestWithCredentials(HttpMethod.Post, url);
            request.Content = JsonContent.Create(content);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        protected async Task<T> PutAsync<T>(string url, object content, CancellationToken cancellationToken = default)
        {
            var request = CreateRequestWithCredentials(HttpMethod.Put, url);
            request.Content = JsonContent.Create(content);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
                ?? throw new ServiceException("La respuesta del servidor está vacía.");
        }

        protected async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            var request = CreateRequestWithCredentials(HttpMethod.Delete, url);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrThrow(response);
        }

        private async Task EnsureSuccessOrThrow(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage = "Error en la solicitud al servidor.";

            try
            {
                var errorObj = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                if (errorObj?.Error != null)
                    errorMessage = errorObj.Error;
            }
            catch
            {
                errorMessage = !string.IsNullOrWhiteSpace(errorContent) ? errorContent : errorMessage;
            }

            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => new ServiceNotFoundException(errorMessage),
                System.Net.HttpStatusCode.Conflict => new ServiceBusinessException(errorMessage),
                System.Net.HttpStatusCode.Unauthorized => new ServiceBusinessException($"No está autorizado para realizar esta acción."),
                System.Net.HttpStatusCode.Forbidden => new ServiceBusinessException($"No tiene permisos para realizar esta acción."),
                _ => new ServiceException(errorMessage)
            };
        }

        private class ErrorResponse
        {
            public string? Error { get; set; }
        }
    }
}
