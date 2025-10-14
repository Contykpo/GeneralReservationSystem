namespace GeneralReservationSystem.Infrastructure
{
    public static class Constants
    {
        public const string AuthenticationScheme = "GeneralReservationSystemCookieScheme";

        public static class CookieNames
        {
            public const string SessionID = "grs_session_id";
        }

        public static class RoleNames
        {
            public const string User = "Customer";
        }
    }
}
