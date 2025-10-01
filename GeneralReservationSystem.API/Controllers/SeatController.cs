using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.DTOs;
using GeneralReservationSystem.Application.Entities;
using GeneralReservationSystem.Application.Repositories.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace GeneralReservationSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatController : ControllerBase
    {
        #region Fields

        private readonly ISeatRepository _seat;

        #endregion

        #region Constructors

        public SeatController(ISeatRepository seat)
        {
            _seat = seat;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a seat by Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OperationResult>> GetById(int id)
        {
            return Ok(await _seat.GetByIdAsync(id));
        }

        /// <summary>
        /// Adds a single seat.
        /// </summary>
        [HttpPost("add")]
        public async Task<ActionResult<OperationResult>> AddAsync([FromBody] Seat seat)
        {
            return Ok(await _seat.AddAsync(seat));
        }

        /// <summary>
        /// Adds multiple seats.
        /// </summary>
        [HttpPost("add-multiple")]
        public async Task<ActionResult<OperationResult>> AddMultipleAsync([FromBody] IEnumerable<Seat> seats)
        {
            return Ok(await _seat.AddMultipleAsync(seats));
        }

        /// <summary>
        /// Updates a single seat.
        /// </summary>
        [HttpPut("update")]
        public async Task<ActionResult<OperationResult>> UpdateAsync([FromBody] Seat seat)
        {
            return Ok(await _seat.UpdateAsync(seat));
        }

        /// <summary>
        /// Updates multiple seats.
        /// </summary>
        [HttpPut("update-multiple")]
        public async Task<ActionResult<OperationResult>> UpdateMultipleAsync([FromBody] IEnumerable<Seat> seats)
        {
            return Ok(await _seat.UpdateMultipleAsync(seats));
        }

        /// <summary>
        /// Deletes a single seat by Id.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<OperationResult>> Delete(int id)
        {
            return Ok(await _seat.DeleteAsync(id));
        }

        /// <summary>
        /// Deletes multiple seats by Ids.
        /// </summary>
        [HttpPost("delete-multiple")]
        public async Task<ActionResult<OperationResult>> DeleteMultipleAsync([FromBody] IEnumerable<int> ids)
        {
            return Ok(await _seat.DeleteMultipleAsync(ids));
        }

        #endregion
    }
}
