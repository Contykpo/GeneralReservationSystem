using GeneralReservationSystem.Application.Entities.Authentication;
using GeneralReservationSystem.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Application.Repositories.Interfaces.Authentication
{
    public interface IRoleRepository
    {
        //----------------------__CREATE__------------------------

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="role">The role to create.</param>
        /// <returns>
        /// A <see cref="Task{OperationResult}"/> representing the asynchronous operation,
        /// with <c>Success</c> if the role was successfully created; otherwise, <c>Failure</c>.
        /// </returns>
        public Task<OperationResult> CreateRoleAsync(ApplicationRole role);

        //----------------------__READ__------------------------

        /// <summary>
        /// Retrieves a role by its unique identifier.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role.</param>
        /// <returns>
        /// A <see cref="Task{OptionalResult{ApplicationRole}}"/> representing the asynchronous operation,
        /// containing the role if found, or <c>NoValue</c> if no role exists with the given ID.
        /// </returns>
        public Task<OptionalResult<ApplicationRole>> GetByGuidAsync(Guid guid);

        /// <summary>
        /// Retrieves a role by its name.
        /// </summary>
        /// <param name="roleName">The name of the role.</param>
        /// <returns>
        /// A <see cref="Task{OptionalResult{ApplicationRole}}"/> representing the asynchronous operation,
        /// containing the role if found, or <c>NoValue</c> if no role exists with the given name.
        /// </returns>
        public Task<OptionalResult<ApplicationRole>> GetByNameAsync(string roleName);

        //----------------------__UPDATE__------------------------

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="role">The role to update.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
        /// with <c>true</c> if the role was successfully updated; otherwise, <c>false</c>.
        /// </returns>
        public Task<OperationResult> UpdateRoleAsync(ApplicationRole role);

        //----------------------__DELETE__------------------------

        /// <summary>
        /// Deletes an existing role.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to delete.</param>
        /// <returns>
        /// A <see cref="Task{Boolean}"/> representing the asynchronous operation,
        /// with <c>true</c> if the role was successfully deleted; otherwise, <c>false</c>.
        /// </returns>
        public Task<OperationResult> DeleteRoleAsync(Guid roleId);
    }
}
