using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    [Route("customerui")]
    [AllowAnonymous] // Allow public access to customer UI
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache for 1 hour
    public class CustomerUIController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IMemoryCache _memoryCache;
        private readonly IWiiChannelService _channelService;
        private readonly ILogger<CustomerUIController> _logger;

        public CustomerUIController(IWebHostEnvironment environment, IMemoryCache memoryCache, IWiiChannelService channelService, ILogger<CustomerUIController> logger)
        {
            _environment = environment;
            _memoryCache = memoryCache;
            _channelService = channelService;
            _logger = logger;
            
            _logger.LogInformation("CustomerUIController initialized - Environment: {Environment}, ContentRoot: {ContentRoot}", 
                _environment.EnvironmentName, _environment.ContentRootPath);
        }

        // Serve the main index page
        [HttpGet("")]
        [HttpGet("index")]
        public IActionResult Index()
        {
            _logger.LogInformation("CustomerUI Index requested - User-Agent: {UserAgent}, IP: {RemoteIP}", 
                Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress);

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "index.html");
            _logger.LogDebug("Looking for index.html at: {FilePath}", filePath);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("CustomerUI index.html not found at: {FilePath}", filePath);
                return NotFound("CustomerUI index.html not found");
            }

            var content = System.IO.File.ReadAllText(filePath);
            
            // Inject version parameters for CSS and JS files based on modification times
            content = InjectAssetVersions(content);
            
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Loaded index.html - Size: {Size} bytes, Last modified: {LastModified}", 
                fileInfo.Length, fileInfo.LastWriteTime);
            
            // NUCLEAR OPTION: Disable ALL caching for development
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            return Content(content, "text/html");
        }

        // Serve test page
        [HttpGet("test")]
        public IActionResult Test()
        {
            var cacheKey = "customerui_test";
            if (_memoryCache.TryGetValue(cacheKey, out string cachedContent))
            {
                return Content(cachedContent, "text/html");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "test.html");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("CustomerUI test.html not found");
            }

            var content = System.IO.File.ReadAllText(filePath);
            _memoryCache.Set(cacheKey, content, TimeSpan.FromMinutes(30));
            
            Response.Headers.CacheControl = "public, max-age=3600";
            return Content(content, "text/html");
        }

        // Serve view HTML files for AJAX loading
        [HttpGet("views/{viewName}")]
        public IActionResult GetView(string viewName)
        {
            _logger.LogInformation("View requested: {ViewName} - User-Agent: {UserAgent}", 
                viewName, Request.Headers.UserAgent.ToString());

            // Sanitize the view name to prevent directory traversal
            if (string.IsNullOrEmpty(viewName) || viewName.Contains("..") || viewName.Contains("/") || viewName.Contains("\\"))
            {
                _logger.LogWarning("Invalid view name attempted: {ViewName} - IP: {RemoteIP}", 
                    viewName, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid view name");
            }

            var cacheKey = $"customerui_view_{viewName}";
            if (_memoryCache.TryGetValue(cacheKey, out string cachedContent))
            {
                _logger.LogDebug("Serving cached view: {ViewName} - Cache key: {CacheKey}, Content length: {Length}", 
                    viewName, cacheKey, cachedContent.Length);
                Response.Headers.CacheControl = "public, max-age=3600";
                return Content(cachedContent, "text/html");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "views", $"{viewName}.html");
            _logger.LogDebug("Looking for view at: {FilePath}", filePath);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("View not found: {ViewName} at {FilePath}", viewName, filePath);
                return NotFound($"View {viewName}.html not found");
            }

            var content = System.IO.File.ReadAllText(filePath);
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Loaded view: {ViewName} - Size: {Size} bytes, Last modified: {LastModified}", 
                viewName, fileInfo.Length, fileInfo.LastWriteTime);
            
            _memoryCache.Set(cacheKey, content, TimeSpan.FromMinutes(30));
            _logger.LogDebug("Cached view: {ViewName} with key: {CacheKey} for 30 minutes", viewName, cacheKey);
            
            Response.Headers.CacheControl = "public, max-age=3600";
            return Content(content, "text/html");
        }

        // Serve CSS files with aggressive cache busting - NO CACHING, always fresh
        [HttpGet("assets/css/{fileName}")]
        public IActionResult GetCss(string fileName)
        {
            _logger.LogInformation("CSS file requested: {FileName} - Referer: {Referer}", 
                fileName, Request.Headers.Referer.ToString());

            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                _logger.LogWarning("Invalid CSS file name attempted: {FileName} - IP: {RemoteIP}", 
                    fileName, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid file name");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "assets", "css", fileName);
            _logger.LogDebug("Looking for CSS file at: {FilePath}", filePath);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("CSS file not found: {FileName} at {FilePath}", fileName, filePath);
                return NotFound($"CSS file {fileName} not found");
            }

            var fileInfo = new FileInfo(filePath);
            var lastModified = fileInfo.LastWriteTimeUtc;
            
            // Read file content fresh every time
            var content = System.IO.File.ReadAllText(filePath);
            _logger.LogInformation("Loaded CSS file: {FileName} - Size: {Size} bytes, Last modified: {LastModified}", 
                fileName, fileInfo.Length, fileInfo.LastWriteTime);
            
            // NUCLEAR OPTION: Disable ALL caching for CSS - always fetch fresh
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.LastModified = lastModified.ToString("R");
            
            return Content(content, "text/css");
        }

        // Serve JavaScript files with aggressive cache busting - NO CACHING, always fresh
        [HttpGet("assets/js/{fileName}")]
        public IActionResult GetJs(string fileName)
        {
            _logger.LogInformation("JavaScript file requested: {FileName} - Referer: {Referer}", 
                fileName, Request.Headers.Referer.ToString());

            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                _logger.LogWarning("Invalid JS file name attempted: {FileName} - IP: {RemoteIP}", 
                    fileName, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid file name");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "assets", "js", fileName);
            _logger.LogDebug("Looking for JS file at: {FilePath}", filePath);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("JS file not found: {FileName} at {FilePath}", fileName, filePath);
                return NotFound($"JavaScript file {fileName} not found");
            }

            var fileInfo = new FileInfo(filePath);
            var lastModified = fileInfo.LastWriteTimeUtc;
            
            // Read file content fresh every time
            var content = System.IO.File.ReadAllText(filePath);
            _logger.LogInformation("Loaded JS file: {FileName} - Size: {Size} bytes, Last modified: {LastModified}", 
                fileName, fileInfo.Length, fileInfo.LastWriteTime);
            
            // NUCLEAR OPTION: Disable ALL caching for JavaScript - always fetch fresh
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.LastModified = lastModified.ToString("R");
            
            return Content(content, "application/javascript");
        }

        // Serve image files
        [HttpGet("assets/images/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            _logger.LogInformation("Image file requested: {FileName} - Referer: {Referer}, User-Agent: {UserAgent}", 
                fileName, Request.Headers.Referer.ToString(), Request.Headers.UserAgent.ToString());

            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                _logger.LogWarning("Invalid image file name attempted: {FileName} - IP: {RemoteIP}", 
                    fileName, HttpContext.Connection.RemoteIpAddress);
                return BadRequest("Invalid file name");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "assets", "images", fileName);
            _logger.LogDebug("Looking for image file at: {FilePath}", filePath);
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Image file not found: {FileName} at {FilePath}", fileName, filePath);
                return NotFound($"Image file {fileName} not found");
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Serving image: {FileName} - Size: {Size} bytes, Type: {ContentType}, Last modified: {LastModified}", 
                fileName, fileInfo.Length, contentType, fileInfo.LastWriteTime);

            // Cache images for 7 days
            Response.Headers.CacheControl = "public, max-age=604800";
            Response.Headers.Expires = DateTime.UtcNow.AddDays(7).ToString("R");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            _logger.LogDebug("Image served successfully: {FileName} - {Size} bytes sent", fileName, fileBytes.Length);
            
            return File(fileBytes, contentType);
        }

        // Serve audio files
        [HttpGet("assets/audio/{fileName}")]
        public IActionResult GetAudio(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return BadRequest("Invalid file name");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "assets", "audio", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Audio file {fileName} not found");
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            };

            // Cache audio files for 7 days
            Response.Headers.CacheControl = "public, max-age=604800";
            Response.Headers.Expires = DateTime.UtcNow.AddDays(7).ToString("R");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType);
        }

        // Get channel configuration for frontend
        [HttpGet("api/channels")]
        public async Task<IActionResult> GetChannelConfiguration()
        {
            _logger.LogInformation("Channel configuration requested - User-Agent: {UserAgent}, IP: {RemoteIP}", 
                Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress);

            try
            {
                var cacheKey = "customerui_channel_config";
                if (_memoryCache.TryGetValue(cacheKey, out string cachedConfig))
                {
                    _logger.LogDebug("Serving cached channel configuration - Cache key: {CacheKey}, Content length: {Length}", 
                        cacheKey, cachedConfig.Length);
                    Response.Headers.CacheControl = "public, max-age=300"; // 5 minutes for channel config
                    return Content(cachedConfig, "application/json");
                }

                _logger.LogDebug("Loading channel configuration from service...");
                var config = await _channelService.GetChannelConfigurationJsonAsync();
                _logger.LogInformation("Channel configuration loaded - Size: {Size} bytes", config.Length);
                
                _memoryCache.Set(cacheKey, config, TimeSpan.FromMinutes(5));
                _logger.LogDebug("Cached channel configuration with key: {CacheKey} for 5 minutes", cacheKey);
                
                Response.Headers.CacheControl = "public, max-age=300";
                return Content(config, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load channel configuration");
                return Json(new { error = "Failed to load channel configuration", details = ex.Message });
            }
        }

        // Get active channels for menu rendering
        [HttpGet("api/channels/active")]
        public async Task<IActionResult> GetActiveChannels()
        {
            _logger.LogInformation("Active channels requested - User-Agent: {UserAgent}, IP: {RemoteIP}", 
                Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress);

            try
            {
                var cacheKey = "customerui_active_channels";
                if (_memoryCache.TryGetValue(cacheKey, out var cachedChannels))
                {
                    _logger.LogDebug("Serving cached active channels - Cache key: {CacheKey}", cacheKey);
                    Response.Headers.CacheControl = "public, max-age=300";
                    return Json(cachedChannels);
                }

                _logger.LogDebug("Loading active channels from service...");
                var channels = await _channelService.GetActiveChannelsAsync();
                _logger.LogInformation("Active channels loaded - Count: {Count}", channels.Count());
                
                foreach (var channel in channels)
                {
                    _logger.LogDebug("Active channel: {ChannelKey} - Name: {Name}, Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}", 
                        channel.ChannelKey, channel.Name, channel.Position, channel.SpriteId, channel.IconPath);
                }
                
                _memoryCache.Set(cacheKey, channels, TimeSpan.FromMinutes(5));
                _logger.LogDebug("Cached active channels with key: {CacheKey} for 5 minutes", cacheKey);
                
                Response.Headers.CacheControl = "public, max-age=300";
                return Json(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load active channels");
                return Json(new { error = "Failed to load active channels", details = ex.Message });
            }
        }

        // Get channel by key for splash screen
        [HttpGet("api/channels/{channelKey}")]
        public async Task<IActionResult> GetChannelByKey(string channelKey)
        {
            _logger.LogInformation("Channel by key requested: {ChannelKey} - User-Agent: {UserAgent}, IP: {RemoteIP}", 
                channelKey, Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress);

            try
            {
                var cacheKey = $"customerui_channel_{channelKey}";
                if (_memoryCache.TryGetValue(cacheKey, out var cachedChannel))
                {
                    _logger.LogDebug("Serving cached channel: {ChannelKey} - Cache key: {CacheKey}", channelKey, cacheKey);
                    Response.Headers.CacheControl = "public, max-age=600"; // 10 minutes for individual channels
                    return Json(cachedChannel);
                }

                _logger.LogDebug("Loading channel from service: {ChannelKey}", channelKey);
                var channel = await _channelService.GetChannelByKeyAsync(channelKey);
                if (channel == null)
                {
                    _logger.LogWarning("Channel not found: {ChannelKey}", channelKey);
                    return NotFound();
                }

                _logger.LogInformation("Channel loaded: {ChannelKey} - Name: {Name}, SpriteId: {SpriteId}, IconPath: {IconPath}, ActionType: {ActionType}", 
                    channel.ChannelKey, channel.Name, channel.SpriteId, channel.IconPath, channel.ActionType);

                _memoryCache.Set(cacheKey, channel, TimeSpan.FromMinutes(10));
                _logger.LogDebug("Cached channel: {ChannelKey} with key: {CacheKey} for 10 minutes", channelKey, cacheKey);
                
                Response.Headers.CacheControl = "public, max-age=600";
                return Json(channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load channel: {ChannelKey}", channelKey);
                return Json(new { error = "Failed to load channel", details = ex.Message });
            }
        }

        // Serve admin interface
        [HttpGet("admin")]
        [HttpGet("admin/channels")]
        public IActionResult AdminChannelManager()
        {
            var filePath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "admin", "channel-manager.html");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Admin interface not found");
            }

            var content = System.IO.File.ReadAllText(filePath);
            return Content(content, "text/html");
        }

        // Helper method to inject version parameters into CSS and JS URLs
        private string InjectAssetVersions(string htmlContent)
        {
            var assetsPath = Path.Combine(_environment.ContentRootPath, "Views", "CustomerUI", "assets");
            
            // Process CSS files
            var cssPath = Path.Combine(assetsPath, "css");
            if (Directory.Exists(cssPath))
            {
                foreach (var cssFile in Directory.GetFiles(cssPath, "*.css"))
                {
                    var fileName = Path.GetFileName(cssFile);
                    var fileInfo = new FileInfo(cssFile);
                    var version = fileInfo.LastWriteTimeUtc.Ticks.ToString("X");
                    
                    // Replace href="/customerui/assets/css/filename.css" with versioned URL
                    var oldPattern = $"href=\"/customerui/assets/css/{fileName}\"";
                    var newPattern = $"href=\"/customerui/assets/css/{fileName}?v={version}\"";
                    htmlContent = htmlContent.Replace(oldPattern, newPattern);
                    
                    _logger.LogDebug("Injected version for CSS: {FileName} -> v={Version}", fileName, version);
                }
            }
            
            // Process JS files
            var jsPath = Path.Combine(assetsPath, "js");
            if (Directory.Exists(jsPath))
            {
                foreach (var jsFile in Directory.GetFiles(jsPath, "*.js"))
                {
                    var fileName = Path.GetFileName(jsFile);
                    var fileInfo = new FileInfo(jsFile);
                    var version = fileInfo.LastWriteTimeUtc.Ticks.ToString("X");
                    
                    // Replace src="/customerui/assets/js/filename.js" with versioned URL
                    var oldPattern = $"src=\"/customerui/assets/js/{fileName}\"";
                    var newPattern = $"src=\"/customerui/assets/js/{fileName}?v={version}\"";
                    htmlContent = htmlContent.Replace(oldPattern, newPattern);
                    
                    _logger.LogDebug("Injected version for JS: {FileName} -> v={Version}", fileName, version);
                }
            }
            
            return htmlContent;
        }
    }
}