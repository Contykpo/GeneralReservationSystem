using GeneralReservationSystem.Application.Common;
using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Interfaces
{
    public interface ISessionRepository
    {
        //----------------------__CREATE__------------------------

        /// <summary>
        /// Creates a new user sessiom.
        /// </summary>
        /// <param name="newSession">The session to create.</param>
        /// <returns>
        /// A <see cref="Task{OperationResult}"/> representing the asynchronous operation,
        /// with <c>Success</c> if the session was successfully created; otherwise, <c>Failure</c>.
        /// </returns>
        public Task<OperationResult> CreateSessionAsync(UserSession newSession);

        //----------------------__READ__------------------------

        /// <summary>
        /// Retrieves a user session by its unique identifier.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <returns>
        /// A <see cref="Task{OptionalResult{UserSession}}"/> representing the asynchronous operation,
        /// containing the session if found, or <c>NoValue</c> if no session exists with the given ID.
        /// </returns>
        public Task<OptionalResult<UserSession>> GetSessionByIdAsync(Guid sessionId);

        /// <summary>
        /// Retrieves all active sessions for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A <see cref="Task{OptionalResult{IList{UserSession}}}"/> representing the asynchronous operation,
        /// containing a list of active sessions for the user.
        /// </returns>
        public Task<OptionalResult<IList<UserSession>>> GetActiveSessionsByUserIdAsync(Guid userId);

        //----------------------__UPDATE__------------------------

        /// <summary>
        /// Updates an existing user session.
        /// </summary>
        /// <param name="updatedSession">The updated session information.</param>
        /// <returns>
        /// A <see cref="Task{OperationResult}"/> representing the asynchronous operation,
        /// with <c>Success</c> if the session was successfully updated; otherwise, <c>Failure</c>.
        /// </returns>
        public Task<OperationResult> UpdateSessionAsync(UserSession updatedSession);

        //----------------------__DELETE__------------------------

        /// <summary>
        /// Deletes a user session by its unique identifier.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session.</param>
        /// <returns>
        /// A <see cref="Task{OperationResult}"/> representing the asynchronous operation,
        /// with <c>Success</c> if the session was successfully deleted; otherwise, <c>Failure</c>.
        /// </returns>
        public Task<OperationResult> DeleteSessionAsync(Guid sessionId);
    }
}
