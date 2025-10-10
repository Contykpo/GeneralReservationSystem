using System.Globalization;
using System.Text;

namespace GeneralReservationSystem.Application.Common
{
    public static class TextUtils
    {
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var normalized = input.Trim().ToUpperInvariant();
            normalized = normalized.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
