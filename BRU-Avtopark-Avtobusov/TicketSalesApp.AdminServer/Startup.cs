using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Services.Implementations;
using TicketSalesApp.Services.Interfaces;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting;
using App.Metrics.AspNetCore;

using App.Metrics.AspNetCore.Endpoints;
using Prometheus;
using Serilog;

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
                // Configure DbContext
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
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to configure database. Application startup will be aborted.");
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
            });

            // Configure Authentication
            try
            {
                var jwtSettings = Configuration.GetSection("JwtSettings");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ??
                    throw new InvalidOperationException("JWT Secret is not configured in appsettings.json"));

                services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = !Environment.IsDevelopment(); // Don't require HTTPS in development
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero,
                        RoleClaimType = "role"
                    };
                });

                // Add authorization policies
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                        policy.RequireClaim("role", "1"));
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

            // Add QR Authentication Service
            services.AddMemoryCache(); // Required for QR login session management
            services.AddScoped<IQRAuthenticationService, QRAuthenticationService>();
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors("AllowAll");
            });

            // Initialize database
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

                    // Ensure we have write permissions
                    try
                    {
                        using var test = File.OpenWrite(dbPath);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Cannot write to database file at {dbPath}. Please check permissions.", ex);
                    }
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
    }
}
