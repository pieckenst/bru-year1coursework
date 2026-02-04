namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    /// <summary>
    /// Service for notifying when user roles change to invalidate caches
    /// </summary>
    public interface IUserRoleChangeNotificationService
    {
        /// <summary>
        /// Notify that a user's roles have changed
        /// </summary>
        /// <param name="userId">User ID whose roles changed</param>
        Task NotifyUserRoleChangedAsync(long userId);

        /// <summary>
        /// Notify that multiple users' roles have changed
        /// </summary>
        /// <param name="userIds">User IDs whose roles changed</param>
        Task NotifyUserRolesChangedAsync(IEnumerable<long> userIds);

        /// <summary>
        /// Notify that a role's permissions have changed (affects all users with that role)
        /// </summary>
        /// <param name="roleId">Role ID whose permissions changed</param>
        Task NotifyRolePermissionsChangedAsync(Guid roleId);

        /// <summary>
        /// Notify that all role data should be invalidated (major system changes)
        /// </summary>
        Task NotifyAllRoleDataChangedAsync();
    }
}