namespace GeneralReservationSystem.Web.Client.Helpers
{
    public class ApiBaseUrlProvider(string baseUrl) : IApiBaseUrlProvider
    {
        public string BaseUrl { get; } = baseUrl;
    }
}