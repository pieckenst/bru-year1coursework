using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{ // Services/Implementations/AdminActionLogger.cs
public class AdminActionLogger : IAdminActionLogger
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminActionLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminActionLogger(
        AppDbContext context,
        ILogger<AdminActionLogger> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogActionAsync(string userId, string action, string details)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(action))
                throw new ArgumentNullException(nameof(action));

            var httpContext = _httpContextAccessor.HttpContext;
            var log = new AdminActionLog
            {
                UserId = userId,
                Action = action,
                Details = details ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
            };

            await _context.AdminActionLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Admin action logged: {Action} by user {UserId} from {IpAddress}",
                action, userId, log.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging admin action for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AdminActionLog>> GetUserActionsAsync(
        string userId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var query = _context.AdminActionLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user actions for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<AdminActionLog>> GetActionsByTypeAsync(
        string actionType, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrEmpty(actionType))
                throw new ArgumentNullException(nameof(actionType));

            var query = _context.AdminActionLogs
                .AsNoTracking()
                .Where(l => l.Action == actionType);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving actions by type {ActionType}", actionType);
            throw;
        }
    }
}}