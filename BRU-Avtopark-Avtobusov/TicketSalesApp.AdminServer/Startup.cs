using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.AdminServer.Security;
using TicketSalesApp.AdminServer.Authentication;
using TicketSalesApp.AdminServer.Authorization;
using TicketSalesApp.AdminServer.Hubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Implementations;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.AdminServer.Services;
using TicketSalesApp.Services.Interfaces;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting;
using App.Metrics.AspNetCore;
using App.Metrics.AspNetCore.Endpoints;
using Prometheus;
using Serilog;
using Fido2NetLib;
using Hangfire;
using Hangfire.Storage.SQLite;
using TicketSalesApp.AdminServer.Configuration;

namespace TicketSalesApp.AdminServer
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;

            // Create a temporary logger factory for startup
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();

                if (environment.IsDevelopment())
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                }
            });

            _logger = loggerFactory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration => _configuration;
        public IWebHostEnvironment Environment => _environment;

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure logging first
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.AddDebug();
                if (Environment.IsDevelopment())
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                }
            });

            services.AddControllers(options =>
            {
                options.RespectBrowserAcceptHeader = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
            });

            try
            {
                // Configure primary database (SQL) - keep existing approach
                var provider = Configuration.GetValue<string>("DatabaseProvider", "SQLite");
                var dbPath = Path.Combine(AppContext.BaseDirectory, "ticketsales.db");

                if (provider == "SQLite")
                {
                    // Ensure the directory exists
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory!);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlite($"Data Source={dbPath}");
                        // Enable detailed errors in development
                        if (Environment.IsDevelopment())
                        {
                            options.EnableDetailedErrors();
                            options.EnableSensitiveDataLogging();
                        }
                    }, contextLifetime: ServiceLifetime.Scoped,
                       optionsLifetime: ServiceLifetime.Singleton);

                    services.AddScoped(sp =>
                        new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>(), "SQLite"));
                }
                else
                {
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                            b => b.MigrationsAssembly("TicketSalesApp.Core"));
                        // Enable detailed errors in development
                        if (Environment.IsDevelopment())
                        {
                            options.EnableDetailedErrors();
                            options.EnableSensitiveDataLogging();
                        }
                    }, contextLifetime: ServiceLifetime.Scoped,
                       optionsLifetime: ServiceLifetime.Singleton);

                    services.AddScoped(sp =>
                        new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>(), "SQLServer"));
                }

                // Add enhanced database services (MongoDB, Redis, Repository pattern)
                services.AddDatabaseServices(Configuration);
                
                _logger.LogInformation("Enhanced database architecture configured successfully with primary SQL database and MongoDB/Redis support");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure enhanced database architecture. Application startup will be aborted.");
                throw;
            }

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Disposition", "Authorization");
                });

                // Specific CORS policy for SignalR
                options.AddPolicy("SignalRCors", builder =>
                {
                    builder
                        .WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:5000", "https://localhost:5001")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Required for SignalR
                });
            });

            // Configure Authentication
            try
            {
                // Configure JWT Authentication
                var jwtSettings = Configuration.GetSection("JwtSettings");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ??
                    throw new InvalidOperationException("JWT Secret is not configured in appsettings.json"));

                // Configure Windows Authentication
                services.AddAuthentication(options =>
                {
                    // Set default schemes
                    options.DefaultAuthenticateScheme = NegotiateDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = NegotiateDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = !Environment.IsDevelopment();
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = "role"
                    };
                })


                .AddNegotiate(options =>
                {
                    // Require either Kerberos or NTLM with enhanced security
                    options.PersistKerberosCredentials = false;
                    options.PersistNtlmCredentials = false;
                    options.Events = new NegotiateEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var ex = context.Exception;

                            Console.WriteLine("===== NEGOTIATE AUTH FAILURE =====");
                            Console.WriteLine($"Type: {ex.GetType().FullName}");
                            Console.WriteLine($"Message: {ex.Message}");
                            Console.WriteLine($"Inner: {ex.InnerException?.GetType().FullName}");
                            Console.WriteLine($"Inner message: {ex.InnerException?.Message}");
                            Console.WriteLine(ex.ToString());
                            Console.WriteLine("==================================");

                            return Task.CompletedTask;
                        }
                    };
                })

                // Add Windows Authentication (Negotiate)
                .AddNegotiate("Windows",options =>
                {
                    // Require either Kerberos or NTLM with enhanced security
                    options.PersistKerberosCredentials=false;
                    options.PersistNtlmCredentials=false;
                    options.Events = new NegotiateEvents
        {
            OnAuthenticationFailed = context =>
            {
                var ex = context.Exception;

                Console.WriteLine("===== NEGOTIATE AUTH FAILURE =====");
                Console.WriteLine($"Type: {ex.GetType().FullName}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Inner: {ex.InnerException?.GetType().FullName}");
                Console.WriteLine($"Inner message: {ex.InnerException?.Message}");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("==================================");

                return Task.CompletedTask;
            }
        };
                });

                // Add our custom authorization handlers
                services.AddSingleton<IAuthorizationHandler, WindowsAuthSecurityHandler>();
                services.AddScoped<IAuthorizationHandler, DatabaseRoleAuthorizationHandler>();
                services.AddHttpContextAccessor();

                // Add authorization policies
                services.AddAuthorization(options =>
                {
                    // Configure database-backed authorization policies
                    TicketSalesApp.AdminServer.Configuration.AuthorizationPolicies.ConfigurePolicies(options);
                        
                    // Windows Authentication policy with enhanced security
                    options.AddPolicy("WindowsAuth", policy =>
                    {
                        policy.RequireAuthenticatedUser()
                              .AddAuthenticationSchemes("Windows")
                              .AddRequirements(new WindowsAuthSecurityRequirement());
                    });

                    // Combined policy that allows either JWT or Windows auth
                    options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "Windows")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure authentication. Application startup will be aborted.");
                throw;
            }

            services.AddSerilog();

            // Register Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ITicketSalesService, TicketSalesService>();
            services.AddScoped<IDataService, DataService>();
            
            // Register Business Services
            services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IAuthenticationBusinessService, TicketSalesApp.AdminServer.Services.AuthenticationBusinessService>();
            services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IWindowsAuthBusinessService, TicketSalesApp.AdminServer.Services.WindowsAuthBusinessService>();
            
            // Add memory cache and distributed cache
            services.AddMemoryCache();
            services.AddDistributedMemoryCache(); // Required for Fido2 library
            
            // Configure Redis for session state and distributed caching
            ConfigureRedisServices(services);
            
            // Configure WebAuthn (FIDO2) services
            ConfigureWebAuthn(services);
            
            services.AddHttpContextAccessor();
            // Rate Limiting
            services.Configure<RateLimitOptions>(Configuration.GetSection(RateLimitOptions.RateLimit));
            var rateLimitOptions = new RateLimitOptions();
            Configuration.GetSection(RateLimitOptions.RateLimit).Bind(rateLimitOptions);

            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("fixed", opt =>
                {
                    opt.PermitLimit = rateLimitOptions.PermitLimit;
                    opt.Window = TimeSpan.FromSeconds(rateLimitOptions.Window);
                    opt.QueueLimit = rateLimitOptions.QueueLimit;
                });

                options.AddSlidingWindowLimiter("sliding", opt =>
                {
                    opt.PermitLimit = rateLimitOptions.PermitLimit;
                    opt.Window = TimeSpan.FromSeconds(rateLimitOptions.Window);
                    opt.SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow;
                    opt.QueueLimit = rateLimitOptions.QueueLimit;
                });

                options.AddTokenBucketLimiter("token", opt =>
                {
                    opt.TokenLimit = rateLimitOptions.TokenLimit;
                    opt.QueueLimit = rateLimitOptions.QueueLimit;
                    opt.TokensPerPeriod = rateLimitOptions.TokensPerPeriod;
                    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitOptions.ReplenishmentPeriod);
                });

                options.AddConcurrencyLimiter("concurrency", opt =>
                {
                    opt.PermitLimit = rateLimitOptions.ConcurrencyLimit;
                    opt.QueueLimit = rateLimitOptions.QueueLimit;
                });

                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString();
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });

            // Metrics configuration
            var metrics = AppMetrics.CreateDefaultBuilder()
                .OutputMetrics.AsPrometheusPlainText()
                .OutputMetrics.AsPrometheusProtobuf()
                .Build();

            services.AddMetrics(metrics);
            services.AddMetricsTrackingMiddleware();
            services.AddMetricsEndpoints();

            // Configure metrics middleware and endpoints
            services.AddMetricsEndpoints(options =>
            {
                options.MetricsEndpointEnabled = true;
                options.MetricsTextEndpointEnabled = true;
                options.EnvironmentInfoEndpointEnabled = true;
            });

            // Configure metrics formatting
            services.Configure<MetricsOptions>(options =>
            {
                options.DefaultContextLabel = "TicketSalesApp.Metrics";
                options.Enabled = true;
            });

            // Add metrics reporting
            services.AddMetricsReportingHostedService();

            // Add AdminActionLogger
            services.AddScoped<IAdminActionLogger, AdminActionLogger>();

            // Configure Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TicketSalesApp Admin API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Register services
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRoleCacheService, RoleCacheService>();
            services.AddScoped<IUserRoleChangeNotificationService, UserRoleChangeNotificationService>();

            // Add QR Authentication Service
            services.AddMemoryCache(); // Required for QR login session management
            services.AddScoped<IQRAuthenticationService, QRAuthenticationService>();

            // Configure SignalR with Redis backplane
            ConfigureSignalR(services);

            // Register notification service
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationHubContext, TicketSalesApp.AdminServer.Services.SignalRNotificationHubContext>();

            // Configure Hangfire BEFORE Export Services (required for IBackgroundJobClient)
            ConfigureHangfire(services);

            // Configure Export Services (depends on Hangfire)
            ConfigureExportServices(services);
            
            // Register data synchronization service for SQL-MongoDB sync
            services.AddScoped<IDataSynchronizationService, DataSynchronizationService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext context)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketSalesApp Admin API v1"));
            }
            else
            {
                // Add rate limiting middleware
                app.UseRateLimiter();

                // Add metrics middleware
                app.UseMetricsAllMiddleware();
                app.UseMetricsAllEndpoints();

                // Use the Prometheus middleware
                app.UseMetricServer();
                app.UseHttpMetrics();
                // Global error handling
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "application/json";

                        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                        var exception = exceptionHandlerPathFeature?.Error;

                        _logger.LogError(exception, "An unhandled exception occurred.");

                        var response = new
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = env.IsDevelopment() ? exception?.Message : "An error occurred processing your request.",
                            Details = env.IsDevelopment() ? exception?.StackTrace : null
                        };

                        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    });
                });
            }

            // In production, enforce HTTPS
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            // Add response caching middleware (after routing, before CORS)
            app.UseMiddleware<TicketSalesApp.AdminServer.Middleware.ResponseCachingMiddleware>();

            // Configure CORS after routing but before authentication
            app.UseCors("AllowAll");

            // Handle preflight requests
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                    context.Response.Headers.Add("Access-Control-Max-Age", "86400");
                    context.Response.StatusCode = 200;
                    return;
                }
                await next();
            });

            // Add session middleware (must be before authentication)
            app.UseSession();

            // Add Windows authentication logging middleware (before authentication)
            app.UseMiddleware<TicketSalesApp.AdminServer.Authentication.WindowsAuthLoggingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            // Configure Hangfire Dashboard (after authentication)
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            // Configure recurring background jobs
            ScheduledJobsConfiguration.ConfigureRecurringJobs();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors("AllowAll");
                
                // Map SignalR hub with specific CORS policy
                endpoints.MapHub<NotificationHub>("/hubs/notifications")
                    .RequireCors("SignalRCors");
            });

            // Initialize database (existing approach) + enhanced services
            try
            {
                // Ensure database is created and migrations are applied
                var provider = Configuration.GetValue<string>("DatabaseProvider", "SQLite");

                if (provider == "SQLite")
                {
                    // Ensure SQLite database file exists
                    var dbPath = Path.Combine(AppContext.BaseDirectory, "ticketsales.db");
                    var directory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory!);
                    }

                    // Create database file if it doesn't exist
                    if (!File.Exists(dbPath))
                    {
                        using var fs = File.Create(dbPath);
                        fs.Close();
                    }

                    // Note: Permission checks are handled by Entity Framework during initialization
                    // Attempting to open the file here can cause locking conflicts
                }

                // Initialize database with retries
                const int maxRetries = 3;
                var retryCount = 0;

                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await DbInitializer.InitializeAsync(context, provider, _logger);
                            
                            // Also initialize enhanced database services
                            await app.ApplicationServices.InitializeDatabasesAsync();
                            
                            _logger.LogInformation("Database initialization completed successfully with enhanced services");
                            break; // Success
                        }
                        catch (Exception ex) when (retryCount < maxRetries)
                        {
                            retryCount++;
                            _logger.LogWarning(ex, $"Database initialization attempt {retryCount} of {maxRetries} failed. Retrying...");
                            await Task.Delay(1000 * retryCount); // Exponential backoff
                        }
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to initialize database. Application startup will be aborted.");
                throw;
            }
        }

        /// <summary>
        /// Configure SignalR with Redis backplane and JWT authentication
        /// </summary>
        private void ConfigureSignalR(IServiceCollection services)
        {
            try
            {
                var signalRSettings = Configuration.GetSection("SignalR");
                var useRedisBackplane = signalRSettings.GetValue<bool>("UseRedisBackplane", true);
                var redisConnectionString = signalRSettings.GetValue<string>("RedisConnectionString") 
                    ?? Configuration.GetConnectionString("Redis");

                var signalRBuilder = services.AddSignalR(options =>
                {
                    // Configure SignalR options
                    options.EnableDetailedErrors = Environment.IsDevelopment() || 
                        signalRSettings.GetValue<bool>("EnableDetailedErrors", false);
                    
                    // Configure timeouts
                    var clientTimeoutInterval = signalRSettings.GetValue<string>("ClientTimeoutInterval");
                    if (!string.IsNullOrEmpty(clientTimeoutInterval) && TimeSpan.TryParse(clientTimeoutInterval, out var clientTimeout))
                    {
                        options.ClientTimeoutInterval = clientTimeout;
                    }

                    var keepAliveInterval = signalRSettings.GetValue<string>("KeepAliveInterval");
                    if (!string.IsNullOrEmpty(keepAliveInterval) && TimeSpan.TryParse(keepAliveInterval, out var keepAlive))
                    {
                        options.KeepAliveInterval = keepAlive;
                    }

                    // Configure maximum message size (default 32KB)
                    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB

                    _logger.LogInformation("SignalR configured with detailed errors: {DetailedErrors}, " +
                        "client timeout: {ClientTimeout}, keep alive: {KeepAlive}",
                        options.EnableDetailedErrors, options.ClientTimeoutInterval, options.KeepAliveInterval);
                });

                // Configure Redis backplane if enabled
                if (useRedisBackplane && !string.IsNullOrEmpty(redisConnectionString))
                {
                    try
                    {
                        signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
                        {
                            options.Configuration.ChannelPrefix = "TicketSalesApp";
                            
                            // Use specific database for SignalR
                            var redisSettings = Configuration.GetSection("Redis");
                            var signalRDatabase = redisSettings.GetValue<int>("SignalRDatabase", 3);
                            options.Configuration.DefaultDatabase = signalRDatabase;
                        });
                        
                        _logger.LogInformation("SignalR configured with Redis backplane at {RedisConnectionString}", 
                            redisConnectionString);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to configure Redis backplane, falling back to single server mode");
                        useRedisBackplane = false;
                    }
                }
                else
                {
                    _logger.LogInformation("SignalR configured without Redis backplane (single server mode)");
                }

                // Configure JSON serialization for SignalR
                signalRBuilder.AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.PayloadSerializerOptions.WriteIndented = Environment.IsDevelopment();
                });

                _logger.LogInformation("SignalR configuration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure SignalR. Application startup will be aborted.");
                throw;
            }
        }

        /// <summary>
        /// Configure WebAuthn (FIDO2) services
        /// </summary>
        private void ConfigureWebAuthn(IServiceCollection services)
        {
            try
            {
                // Configure WebAuthn settings
                services.Configure<WebAuthnSettings>(Configuration.GetSection(WebAuthnSettings.SectionName));
                var webAuthnSettings = new WebAuthnSettings();
                Configuration.GetSection(WebAuthnSettings.SectionName).Bind(webAuthnSettings);

                // Configure FIDO2 library
                services.AddFido2(options =>
                {
                    options.ServerDomain = webAuthnSettings.ServerDomain;
                    options.ServerName = webAuthnSettings.ServerName;
                    options.Origins = new HashSet<string>(webAuthnSettings.Origins);
                    options.TimestampDriftTolerance = webAuthnSettings.TimestampDriftTolerance;
                    options.ChallengeSize = webAuthnSettings.ChallengeSize;
                })
                .AddCachedMetadataService(config =>
                {
                    // Configure metadata service for authenticator validation
                    config.AddFidoMetadataRepository();
                });

                // Register WebAuthn service
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IWebAuthnService, TicketSalesApp.AdminServer.Services.WebAuthnService>();
                
                // Register TOTP service
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.ITotpService, TicketSalesApp.AdminServer.Services.TotpService>();

                _logger.LogInformation("WebAuthn configured for domain {ServerDomain} with origins: {Origins}", 
                    webAuthnSettings.ServerDomain, string.Join(", ", webAuthnSettings.Origins));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure WebAuthn. Application startup will be aborted.");
                throw;
            }
        }

        /// <summary>
        /// Configure Hangfire for background job processing
        /// </summary>
        private void ConfigureHangfire(IServiceCollection services)
        {
            try
            {
                // Configure Hangfire settings
                var hangfireSettings = new HangfireSettings();
                Configuration.GetSection(HangfireSettings.SectionName).Bind(hangfireSettings);

                // Configure Hangfire for background job processing
                var hangfireConnectionString = Configuration.GetConnectionString("Hangfire") 
                    ?? Configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=hangfire.db";

                services.AddHangfire(config =>
                {
                    config.UseStorage(new Hangfire.Storage.SQLite.SQLiteStorage(hangfireConnectionString));
                    config.UseSimpleAssemblyNameTypeSerializer();
                    config.UseRecommendedSerializerSettings();
                });

                services.AddHangfireServer(options =>
                {
                    options.WorkerCount = hangfireSettings.WorkerCount > 0 
                        ? hangfireSettings.WorkerCount 
                        : System.Environment.ProcessorCount;
                    options.Queues = hangfireSettings.Queues ?? new[] { "default", "exports", "notifications", "maintenance" };
                    
                    // Configure retry policies
                    options.ServerTimeout = TimeSpan.FromMinutes(5);
                    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
                    options.ServerCheckInterval = TimeSpan.FromMinutes(1);
                });

                // Register background job services
                services.AddScoped<NotificationBackgroundService>();
                services.AddScoped<MaintenanceBackgroundService>();

                _logger.LogInformation("Hangfire configured successfully with SQLite storage and {WorkerCount} workers", 
                    hangfireSettings.WorkerCount > 0 ? hangfireSettings.WorkerCount : System.Environment.ProcessorCount);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure Hangfire. Application startup will be aborted.");
                throw;
            }
        }

        /// <summary>
        /// Configure Export Services
        /// </summary>
        private void ConfigureExportServices(IServiceCollection services)
        {
            try
            {
                // Configure Export Options
                services.Configure<TicketSalesApp.AdminServer.Configuration.ExportOptions>(
                    Configuration.GetSection(TicketSalesApp.AdminServer.Configuration.ExportOptions.SectionName));

                // Register Export Services
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IExportService, TicketSalesApp.AdminServer.Services.ExportService>();
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IExportDataProvider, TicketSalesApp.AdminServer.Services.ExportDataProvider>();
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IExportFileWriter, TicketSalesApp.AdminServer.Services.ExportFileWriter>();
                services.AddScoped<TicketSalesApp.AdminServer.Services.Interfaces.IExportProgressTracker, TicketSalesApp.AdminServer.Services.ExportProgressTracker>();

                // Register Export Background Service
                services.AddScoped<TicketSalesApp.AdminServer.Services.ExportBackgroundService>();

                // Register Export Cleanup Service
                services.AddHostedService<TicketSalesApp.AdminServer.Services.ExportCleanupService>();

                _logger.LogInformation("Export services configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure export services. Application startup will be aborted.");
                throw;
            }
        }

        /// <summary>
        /// Configure Redis services for session state, caching, and SignalR backplane
        /// </summary>
        private void ConfigureRedisServices(IServiceCollection services)
        {
            try
            {
                var redisSettings = Configuration.GetSection("Redis");
                var connectionString = redisSettings.GetValue<string>("ConnectionString") 
                    ?? Configuration.GetConnectionString("Redis") 
                    ?? "localhost:6379";

                // Configure Redis session state
                services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SecurePolicy = Environment.IsDevelopment() 
                        ? CookieSecurePolicy.SameAsRequest 
                        : CookieSecurePolicy.Always;
                });

                // Configure Redis distributed cache for sessions
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                    options.InstanceName = redisSettings.GetValue<string>("InstanceName") ?? "TicketSalesApp";
                    
                    // Use specific database for sessions
                    var sessionDatabase = redisSettings.GetValue<int>("SessionDatabase", 2);
                    options.ConfigurationOptions = StackExchange.Redis.ConfigurationOptions.Parse(connectionString);
                    options.ConfigurationOptions.DefaultDatabase = sessionDatabase;
                });

                // Register response caching services
                services.AddScoped<IResponseCacheService, ResponseCacheService>();
                services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
                services.AddScoped<ICacheWarmupService, CacheWarmupService>();

                // Register Wii Channel Service
                services.AddScoped<IWiiChannelService, WiiChannelService>();

                // Register cache warmup background service
                services.AddHostedService<TicketSalesApp.AdminServer.Services.CacheWarmupBackgroundService>();

                _logger.LogInformation("Redis services configured successfully for session state and caching");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure Redis services. Application startup will be aborted.");
                throw;
            }
        }
    }
}