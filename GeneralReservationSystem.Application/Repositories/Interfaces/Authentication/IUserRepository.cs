using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Infrastructure.Common;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
	public interface IUserRepository
	{
		//----------------------__READ__------------------------

		/// <summary>
		/// Retrieves a user by their unique identifier.
		/// </summary>
		/// <param name="guid">The unique identifier of the user.</param>
		/// <returns>
		/// A <see cref="Task{ApplicationUser}"/> representing the asynchronous operation,
		/// containing the user if found, or <c>null</c> if no user exists with the given GUID.
		/// </returns>
		public Task<OptionalResult<ApplicationUser>> GetByGuidAsync(Guid guid);

		/// <summary>
		/// Retrieves a user by their email address.
		/// </summary>
		/// <param name="email">The email address of the user.</param>
		/// <returns>
		/// A <see cref="Task{ApplicationUser}"/> representing the asynchronous operation,
		/// containing the user if found, or <c>null</c> if no user exists with the given email.
		/// </returns>
		public Task<OptionalResult<ApplicationUser>> GetByEmailAsync(string email);

		/// <summary>
		/// Checks if a user exists with the specified email.
		/// </summary>
		/// <param name="email">The email address to check.</param>
		/// <returns>
		/// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
		/// with <c>true</c> if a user exists with the specified email; otherwise, <c>false</c>.
		/// </returns>
		public Task<OptionalResult<bool>> ExistsWithEmailAsync(string email);

		/// <summary>
		/// Retrieves all users that have the specified role.
		/// </summary>
		/// <param name="roleName">The name of the role.</param>
		/// <returns>
		/// A <see cref="Task{IList{ApplicationUser}}"/> representing the asynchronous operation,
		/// containing a list of users with the specified role.
		/// </returns>
		public Task<OptionalResult<IList<ApplicationUser>>> GetWithRoleAsync(string roleName);

		/// <summary>
		/// Retrieves all roles assigned to the user identified by <paramref name="userGuid"/>.
		/// </summary>
		/// <param name="userGuid">The unique identifier of the user.</param>
		/// <returns>
		/// A <see cref="Task{IList{ApplicationRole}}"/> representing the asynchronous operation,
		/// containing a list of roles assigned to the user.
		/// </returns>
		public Task<OptionalResult<IList<ApplicationRole>>> GetUserRolesAsync(Guid userGuid);

		/// <summary>
		/// Retrieves all users in the system.
		/// </summary>
		/// <returns>
		/// A <see cref="Task{IList{ApplicationUser}}"/> representing the asynchronous operation,
		/// containing a list of all users.
		/// </returns>
		public Task<OptionalResult<IList<ApplicationUser>>> GetAllAsync();

		/// <summary>
		/// Retrieves users that satisfy a specified filter.
		/// </summary>
		/// <param name="predicate">A function to test each user for a condition.</param>
		/// <returns>
		/// A <see cref="Task{IList{ApplicationUser}}"/> representing the asynchronous operation,
		/// containing a list of users that match the filter.
		/// </returns>
		public Task<OptionalResult<IList<ApplicationUser>>> FilterAsync(Func<ApplicationUser, bool> predicate);

		//----------------------__CREATE__------------------------

		/// <summary>
		/// Creates a new user and optionally assigns roles to them.
		/// </summary>
		/// <param name="newUser">The user to create.</param>
		/// <param name="userRoles">A collection of roles to assign to the new user.</param>
		/// <returns>
		/// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
		/// with <c>true</c> if the user was successfully created; otherwise, <c>false</c>.
		/// </returns>
		public Task<OperationResult> CreateUserAsync(ApplicationUser newUser, IEnumerable<ApplicationRole> userRoles);

		//----------------------__UPDATE__------------------------

		/// <summary>
		/// Updates an existing user's information.
		/// </summary>
		/// <param name="user">The user with updated information.</param>
		/// <returns>
		/// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
		/// with <c>true</c> if the user was successfully updated; otherwise, <c>false</c>.
		/// </returns>
		public Task<OperationResult> UpdateUserAsync(ApplicationUser user);

		/// <summary>
		/// Adds the specified role to the user identified by <paramref name="userGuid"/>.
		/// </summary>
		/// <param name="userGuid">The unique identifier of the user.</param>
		/// <param name="roleName">The name of the role to assign to the user.</param>
		/// <returns>
		/// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
		/// with <c>true</c> if the role was successfully added; otherwise, <c>false</c>.
		/// </returns>
		public Task<OperationResult> AddRoleAsync(Guid userGuid, string roleName);

		/// <summary>
		/// Removes the specified role from the user identified by <paramref name="userGuid"/>.
		/// </summary>
		/// <param name="userGuid">The unique identifier of the user.</param>
		/// <param name="roleName">The name of the role to remove from the user.</param>
		/// <returns>
		/// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
		/// with <c>true</c> if the role was successfully removed; otherwise, <c>false</c>.
		/// </returns>
		public Task<OperationResult> RemoveRoleAsync(Guid userGuid, string roleName);
	}
}