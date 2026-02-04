using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Middleware
{
    /// <summary>
    /// Middleware for automatic response caching based on request patterns
    /// </summary>
    public class ResponseCachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ResponseCachingMiddleware> _logger;
        private readonly HashSet<string> _cacheableEndpoints;
        private readonly HashSet<string> _cacheableMethods;

        public ResponseCachingMiddleware(
            RequestDelegate next,
            IServiceProvider serviceProvider,
            ILogger<ResponseCachingMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Define cacheable endpoints (GET requests only)
            _cacheableEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "/api/buses",
                "/api/routes", 
                "/api/users",
                "/api/employees",
                "/api/roles",
                "/api/ticketsales"
            };
            
            // Only cache GET requests
            _cacheableMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GET"
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only cache GET requests to specific endpoints
            if (!ShouldCache(context.Request))
            {
                await _next(context);
                return;
            }

            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IResponseCacheService>();

            var cacheKey = GenerateCacheKey(context.Request);
            var tags = GenerateCacheTags(context.Request);

            try
            {
                // Try to get cached response
                var cachedResponse = await cacheService.GetAsync<CachedResponse>(cacheKey);
                if (cachedResponse != null)
                {
                    _logger.LogDebug("Serving cached response for {Method} {Path}", 
                        context.Request.Method, context.Request.Path);
                    
                    // Serve cached response
                    context.Response.StatusCode = cachedResponse.StatusCode;
                    context.Response.ContentType = cachedResponse.ContentType;
                    
                    // Set cache headers
                    context.Response.Headers.Add("X-Cache", "HIT");
                    context.Response.Headers.Add("X-Cache-Key", cacheKey);
                    
                    await context.Response.WriteAsync(cachedResponse.Content);
                    return;
                }

                // Cache miss - execute request and cache response
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                // Only cache successful responses
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    var responseContent = await new StreamReader(responseBodyStream).ReadToEndAsync();
                    
                    var cachedResponseData = new CachedResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        ContentType = context.Response.ContentType ?? "application/json",
                        Content = responseContent,
                        CachedAt = DateTime.UtcNow
                    };

                    // Cache the response
                    await cacheService.SetAsync(cacheKey, cachedResponseData, null, tags);
                    
                    // Set cache headers
                    context.Response.Headers.Add("X-Cache", "MISS");
                    context.Response.Headers.Add("X-Cache-Key", cacheKey);
                    
                    _logger.LogDebug("Cached response for {Method} {Path} with key {CacheKey}", 
                        context.Request.Method, context.Request.Path, cacheKey);
                }

                // Copy cached response to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in response caching middleware for {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
                
                // Continue without caching on error
                await _next(context);
            }
        }

        private bool ShouldCache(HttpRequest request)
        {
            // Only cache GET requests
            if (!_cacheableMethods.Contains(request.Method))
                return false;

            // Check if endpoint is cacheable
            var path = request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Check exact matches first
            if (_cacheableEndpoints.Contains(path))
                return true;
            
            // Check if path starts with any cacheable endpoint
            return _cacheableEndpoints.Any(endpoint => path.StartsWith(endpoint.ToLowerInvariant()));
        }

        private string GenerateCacheKey(HttpRequest request)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append("response:");
            keyBuilder.Append(request.Method.ToLowerInvariant());
            keyBuilder.Append(":");
            keyBuilder.Append(request.Path.Value?.ToLowerInvariant() ?? "");
            
            // Include query parameters in cache key
            if (request.QueryString.HasValue)
            {
                // Sort query parameters for consistent cache keys
                var queryParams = request.Query
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Key}={string.Join(",", kvp.Value.ToArray())}")
                    .ToArray();
                
                if (queryParams.Length > 0)
                {
                    keyBuilder.Append("?");
                    keyBuilder.Append(string.Join("&", (IEnumerable<string>)queryParams));
                }
            }
            
            return keyBuilder.ToString();
        }

        private string[] GenerateCacheTags(HttpRequest request)
        {
            var path = request.Path.Value?.ToLowerInvariant() ?? "";
            var tags = new List<string>();
            
            // Add entity-specific tags based on endpoint
            if (path.StartsWith("/api/buses"))
            {
                tags.AddRange(new[] { "buses", "bus-list" });
            }
            else if (path.StartsWith("/api/routes"))
            {
                tags.AddRange(new[] { "routes", "route-list" });
            }
            else if (path.StartsWith("/api/users"))
            {
                tags.AddRange(new[] { "users", "user-list" });
            }
            else if (path.StartsWith("/api/employees"))
            {
                tags.AddRange(new[] { "employees", "employee-list" });
            }
            else if (path.StartsWith("/api/roles"))
            {
                tags.AddRange(new[] { "roles", "role-list" });
            }
            else if (path.StartsWith("/api/ticketsales"))
            {
                tags.AddRange(new[] { "tickets", "ticket-list", "sales", "sales-list" });
            }
            
            // Add general response tag
            tags.Add("responses");
            
            return tags.ToArray();
        }

        /// <summary>
        /// Cached response data structure
        /// </summary>
        private class CachedResponse
        {
            public int StatusCode { get; set; }
            public string ContentType { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime CachedAt { get; set; }
        }
    }
}