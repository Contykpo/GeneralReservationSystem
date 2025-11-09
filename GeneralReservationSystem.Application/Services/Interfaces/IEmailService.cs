using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendReservationConfirmationAsync(ReservationConfirmationEmailDto dto, CancellationToken cancellationToken = default);
        Task SendEmailAsync(EmailDto dto, CancellationToken cancellationToken = default);
        Task SendNotificationAsync(EmailDto dto, CancellationToken cancellationToken = default);
    }
}
