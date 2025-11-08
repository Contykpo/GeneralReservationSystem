using GeneralReservationSystem.Infrastructure.Helpers;

namespace GeneralReservationSystem.Tests.Integration.Helpers;

public static class AuthenticationHelper
{
    public static string? ExtractJwtTokenFromCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
        {
            return null;
        }

        foreach (string cookie in cookies)
        {
            if (cookie.StartsWith($"{JwtHelper.CookieName}="))
            {
                string[] parts = cookie.Split(';');
                string tokenPart = parts[0];
                string token = tokenPart[(JwtHelper.CookieName.Length + 1)..];
                return token;
            }
        }

        return null;
    }

    public static void AddJwtCookie(HttpRequestMessage request, string token)
    {
        request.Headers.Add("Cookie", $"{JwtHelper.CookieName}={token}");
    }

    public static void SetAuthenticationCookie(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Add("Cookie", $"{JwtHelper.CookieName}={token}");
    }

    public static void ClearAuthenticationCookie(HttpClient client)
    {
        _ = client.DefaultRequestHeaders.Remove("Cookie");
    }
}
