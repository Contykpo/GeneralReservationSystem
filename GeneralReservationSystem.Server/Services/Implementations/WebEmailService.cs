using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Web.Client.Services.Interfaces;

namespace GeneralReservationSystem.Server.Services.Implementations
{
    public class WebEmailService : IClientEmailService
    {
        public Task SendReservationConfirmationAsync(ReservationConfirmationEmailDto dto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(EmailDto dto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendNotificationAsync(EmailDto dto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
