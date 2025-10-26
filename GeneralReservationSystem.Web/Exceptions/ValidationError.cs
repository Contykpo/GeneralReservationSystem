using System.Text.Json.Serialization;

namespace GeneralReservationSystem.Web.Exceptions
{
    public record class ValidationError
    {
        [JsonPropertyName("field")]
        public required string Field { get; init; }
        [JsonPropertyName("error")]
        public required string Message { get; init; }
    }
}
