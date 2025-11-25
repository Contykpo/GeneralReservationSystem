using GeneralReservationSystem.API.Helpers;
using GeneralReservationSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GeneralReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/email")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        [HttpPost("send-confirmation")]
        public async Task<IActionResult> SendReservationEmail([FromBody] ReservationConfirmationEmailDto dto)
        {
            try
            {
                await EmailManager.SendReservationConfirmationAsync(dto.Email, dto.UserName, dto.DepartureStation, dto.ArrivalStation, dto.DepartureTime, dto.SeatNumber);

                return Ok();
            }
            catch (Exception ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("send-notification")]
        public async Task<IActionResult> SendNotificationEmail([FromBody] EmailDto dto)
        {
            try
            {
                await EmailManager.SendNotificationAsync(dto.Email, dto.Subject, dto.Body);

                return Ok();
            }
            catch (Exception ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] EmailDto dto)
        {
            try
            {
                await EmailManager.SendEmailAsync(dto.Email, dto.Subject, dto.Body);

                return Ok();
            }
            catch (Exception ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }
    }
}
