using GeneralReservationSystem.Application.Services.Interfaces;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Web.Client.Services.Implementations
{
    public class EmailService(HttpClient httpClient) : ApiServiceBase(httpClient), IEmailService
    {
        public async Task SendReservationConfirmationAsync(ReservationConfirmationEmailDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/emails/send-confirmation", dto, cancellationToken);
        }

        public async Task SendEmailAsync(EmailDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/emails/send-email", dto, cancellationToken);
        }

        public async Task SendNotificationAsync(EmailDto dto, CancellationToken cancellationToken = default)
        {
            await PostAsync("/api/emails/send-notification", dto, cancellationToken);
        }
    }
}
