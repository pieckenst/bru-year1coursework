using Hangfire;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Background service for processing notification jobs
    /// </summary>
    public class NotificationBackgroundService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            INotificationService notificationService,
            ILogger<NotificationBackgroundService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Send notification to a specific user
        /// </summary>
        [Queue("notifications")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task SendUserNotificationAsync(long userId, string type, object data)
        {
            try
            {
                _logger.LogInformation("Sending notification to user {UserId} of type {Type}", userId, type);
                await _notificationService.SendToUserAsync(userId, type, data);
                _logger.LogInformation("Successfully sent notification to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId} of type {Type}", userId, type);
                throw;
            }
        }

        /// <summary>
        /// Send notification to multiple users
        /// </summary>
        [Queue("notifications")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task SendBulkNotificationAsync(IEnumerable<long> userIds, string type, object data)
        {
            try
            {
                _logger.LogInformation("Sending bulk notification to {UserCount} users of type {Type}", 
                    userIds.Count(), type);

                var tasks = userIds.Select(userId => 
                    _notificationService.SendToUserAsync(userId, type, data));
                
                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Successfully sent bulk notification to {UserCount} users", userIds.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk notification of type {Type}", type);
                throw;
            }
        }

        /// <summary>
        /// Send notification to all connected users
        /// </summary>
        [Queue("notifications")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task SendBroadcastNotificationAsync(string type, object data)
        {
            try
            {
                _logger.LogInformation("Broadcasting notification of type {Type}", type);
                await _notificationService.SendToAllAsync(type, data);
                _logger.LogInformation("Successfully broadcast notification of type {Type}", type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast notification of type {Type}", type);
                throw;
            }
        }

        /// <summary>
        /// Send notification to users in a specific group
        /// </summary>
        [Queue("notifications")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
        public async Task SendGroupNotificationAsync(string groupName, string type, object data)
        {
            try
            {
                _logger.LogInformation("Sending notification to group {GroupName} of type {Type}", groupName, type);
                await _notificationService.SendToGroupAsync(groupName, type, data);
                _logger.LogInformation("Successfully sent notification to group {GroupName}", groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to group {GroupName} of type {Type}", 
                    groupName, type);
                throw;
            }
        }

        /// <summary>
        /// Schedule a delayed notification
        /// </summary>
        public string ScheduleNotificationAsync(long userId, string type, object data, TimeSpan delay)
        {
            _logger.LogInformation("Scheduling notification for user {UserId} with delay {Delay}", 
                userId, delay);
            
            var jobId = BackgroundJob.Schedule<NotificationBackgroundService>(
                service => service.SendUserNotificationAsync(userId, type, data),
                delay);

            _logger.LogInformation("Scheduled notification job {JobId} for user {UserId}", jobId, userId);
            return jobId;
        }

        /// <summary>
        /// Schedule a recurring notification
        /// </summary>
        public void ScheduleRecurringNotificationAsync(string jobId, long userId, string type, object data, string cronExpression)
        {
            _logger.LogInformation("Scheduling recurring notification {JobId} for user {UserId} with cron {Cron}", 
                jobId, userId, cronExpression);
            
            RecurringJob.AddOrUpdate<NotificationBackgroundService>(
                jobId,
                service => service.SendUserNotificationAsync(userId, type, data),
                cronExpression);

            _logger.LogInformation("Scheduled recurring notification job {JobId}", jobId);
        }
    }
}
