using GeneralReservationSystem.Application.Entities.Authentication;

using GeneralReservationSystem.Infrastructure.Common;

namespace GeneralReservationSystem.Infrastructure.Repositories.Interfaces
{
	/// <summary>
	/// Define la interfaz para el acceso y manipulación de sesiones de usuario.
	/// </summary>
	public interface ISessionRepository
	{
		/// <summary>
		/// Obtiene una sesión por su identificador, junto con el usuario asociado.
		/// </summary>
		/// <param name="sessionId">El identificador único de la sesión.</param>
		/// <returns>
		/// Un <see cref="OptionalResult{T}"/> que contiene la sesión y el usuario 
		/// si la sesión existe; en caso contrario, un resultado vacío.
		/// </returns>
		public Task<OptionalResult<(UserSession session, ApplicationUser user)>> GetSessionAsync(Guid sessionId);

		/// <summary>
		/// Obtiene todas las sesiones asociadas a un usuario, sin importar si están activas o no.
		/// </summary>
		/// <param name="userId">El identificador único del usuario.</param>
		/// <returns>
		/// Un <see cref="OptionalResult{T}"/> con la lista de sesiones del usuario,
		/// o vacío si no se encontraron sesiones.
		/// </returns>
		public Task<OptionalResult<IList<UserSession>>> GetAllSessionsForUserAsync(Guid userId);

		/// <summary>
		/// Obtiene únicamente las sesiones activas de un usuario.
		/// </summary>
		/// <param name="userId">El identificador único del usuario.</param>
		/// <returns>
		/// Un <see cref="OptionalResult{T}"/> con la lista de sesiones activas,
		/// o vacío si no se encontraron.
		/// </returns>
		public Task<OptionalResult<IList<UserSession>>> GetActiveSessionsForUserAsync(Guid userId);

		/// <summary>
		/// Crea una nueva sesión para un usuario.
		/// </summary>
		/// <param name="newSession">El objeto <see cref="UserSession"/> que representa la nueva sesión.</param>
		/// <returns>
		/// Un <see cref="OperationResult"/> que indica si la operación fue exitosa o no.
		/// </returns>
		public Task<OperationResult> CreateSessionAsync(UserSession newSession);

		/// <summary>
		/// Actualiza la información de una sesión existente.
		/// </summary>
		/// <param name="session">El objeto <see cref="UserSession"/> con los datos actualizados.</param>
		/// <returns>
		/// Un <see cref="OperationResult"/> que indica si la operación fue exitosa o no.
		/// </returns>
		public Task<OperationResult> UpdateSessionAsync(UserSession session);

		/// <summary>
		/// Revoca (invalida) una sesión específica por su identificador.
		/// </summary>
		/// <param name="sessionId">El identificador único de la sesión.</param>
		/// <returns>
		/// Un <see cref="OperationResult"/> que indica si la operación fue exitosa o no.
		/// </returns>
		public Task<OperationResult> RevokeSessionAsync(Guid sessionId);

		/// <summary>
		/// Revoca todas las sesiones activas de un usuario.
		/// </summary>
		/// <param name="userId">El identificador único del usuario.</param>
		/// <returns>
		/// Un <see cref="OperationResult"/> que indica si la operación fue exitosa o no.
		/// </returns>
		public Task<OperationResult> RevokeAllSessionsAsync(Guid userId);
	}
}