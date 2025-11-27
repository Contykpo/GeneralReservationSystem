using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GeneralReservationSystem.Server.Controllers
{
    [ApiController]
    [Route("api/email")]
    public class EmailController : ControllerBase
    {
        [HttpPost("send-confirmation")]
        [Authorize]
        public async Task<IActionResult> SendReservationEmail([FromBody] ReservationConfirmationEmailDto dto)
        {
            await EmailManager.SendReservationConfirmationAsync(dto.Email, dto.UserName, dto.DepartureStation, dto.ArrivalStation, dto.DepartureTime, dto.SeatNumber);

            return Ok();
        }

        [HttpPost("send-notification")]
        [Authorize]
        public async Task<IActionResult> SendNotificationEmail([FromBody] EmailDto dto)
        {
            await EmailManager.SendNotificationAsync(dto.Email, dto.Subject, dto.Body);

            return Ok();
        }

        [HttpPost("send-email")]
        [Authorize]
        public async Task<IActionResult> SendEmail([FromBody] EmailDto dto)
        {
            await EmailManager.SendEmailAsync(dto.Email, dto.Subject, dto.Body);

            return Ok();
        }
    }
}
