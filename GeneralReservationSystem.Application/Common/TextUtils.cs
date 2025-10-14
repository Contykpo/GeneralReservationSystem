using System.Globalization;
using System.Text;

namespace GeneralReservationSystem.Application.Common
{
    public static class TextUtils
    {
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string normalized = input.Trim().ToUpperInvariant();
            normalized = normalized.Normalize(NormalizationForm.FormKD);
            StringBuilder sb = new();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    _ = sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
