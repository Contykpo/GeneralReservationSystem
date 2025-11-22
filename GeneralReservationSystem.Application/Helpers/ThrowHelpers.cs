namespace GeneralReservationSystem.Application.Helpers
{
    public static class ThrowHelpers
    {
        public static void ThrowIfNull<T>(T? value, string parameterName)
            where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void ThrowIfNullOrWhiteSpace(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("String cannot be null or whitespace", parameterName);
            }
        }
    }
}
