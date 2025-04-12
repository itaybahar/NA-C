using API_Project.Configuration;
using API_Project.Data;
using API_Project.Repositories;
using API_Project.Services;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel with improved certificate handling
            ConfigureKestrel(builder);

            // Configure application services
            ConfigureServices(builder);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        private static void ConfigureKestrel(WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5191, listenOptions =>
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        if (builder.Environment.IsDevelopment())
                        {
                            var possibleLocations = new[]
                            {
                                new { CertPath = Path.Combine(Directory.GetCurrentDirectory(), "localhost.pem"),
                                    KeyPath = Path.Combine(Directory.GetCurrentDirectory(), "localhost-key.pem") },
                                new { CertPath = Path.Combine(AppContext.BaseDirectory, "localhost.pem"),
                                    KeyPath = Path.Combine(AppContext.BaseDirectory, "localhost-key.pem") }
                            };

                            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                            Console.WriteLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");

                            bool certificateFound = false;

                            foreach (var location in possibleLocations)
                            {
                                Console.WriteLine($"Checking for certificates at: {location.CertPath} and {location.KeyPath}");

                                if (File.Exists(location.CertPath) && File.Exists(location.KeyPath))
                                {
                                    try
                                    {
                                        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(location.CertPath, location.KeyPath);
                                        Console.WriteLine($"Certificates loaded successfully from {location.CertPath}");
                                        certificateFound = true;
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error loading certificates from {location.CertPath}: {ex.Message}");
                                    }
                                }
                            }

                            if (!certificateFound)
                            {
                                Console.WriteLine("No certificate files found. Using development certificate instead.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Production environment detected. Using configured certificate.");
                        }
                    });
                });

                // Also add HTTPS endpoint on port 7235 as defined in launchSettings.json
                options.ListenAnyIP(7235, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            });
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
                ?? throw new ArgumentNullException("AuthenticationSettings");
            builder.Services.AddSingleton(authSettings);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            ConfigureDatabase(builder);
            RegisterServices(builder);

            builder.Services.AddMemoryCache();
            ConfigureRateLimiting(builder);
            ConfigureHealthChecks(builder);
            ConfigureSwagger(builder);
            ConfigureCors(builder);

            AuthenticationServiceExtensions.ConfigureAuthentication(builder);
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    )
                )
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging()
            );
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleChangeRequestRepository, RoleChangeRequestRepository>();

            builder.Services.AddScoped<Services.AuthenticationService>();
            builder.Services.AddScoped<Domain_Project.Interfaces.IAuthenticationService, AuthenticationServiceAdapter>();
            builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
        }

        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            builder.Services.AddRateLimiter(options =>
            {
                // Configure global rate limiting
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // Add specific named rate limiter for authentication endpoints
                options.AddPolicy("AuthEndpoints", context =>
                {
                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10, // More restrictive limit for auth endpoints
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                    context.HttpContext.Response.Headers.Append("Retry-After", "60"); // Recommend retry after 60 seconds
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });
        }

        private static void ConfigureHealthChecks(WebApplicationBuilder builder)
        {
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
            }

            builder.Services.AddHealthChecks()
                .AddCheck(
                    "mysql-connection",
                    new DbConnectionHealthCheck(connectionString),
                    tags: new[] { "db", "mysql" },
                    timeout: TimeSpan.FromSeconds(30)
                );
        }

        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Equipment Management API",
                    Version = "v1",
                    Description = "API for Equipment Management System",
                    Contact = new OpenApiContact { Name = "API Support" }
                });

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

                c.CustomSchemaIds(type => type.FullName);
            });
        }

        private static void ConfigureCors(WebApplicationBuilder builder)
        {
            // Get the CORS origins from the CorsSettings section which matches launchSettings.json better
            var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ??
                new[] { "https://localhost:7176", "http://localhost:5176" };

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("https://localhost:7176", "http://localhost:5176")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Configure Swagger UI for development
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Management API v1");
                    c.RoutePrefix = "swagger"; // Changed from empty to "swagger"
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                    c.EnableDeepLinking();
                    c.DisplayRequestDuration();
                });

                // Add a redirect from root to swagger UI (after Swagger middleware is set up)
                app.MapGet("/", context => {
                    context.Response.Redirect("/swagger");
                    return Task.CompletedTask;
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Security headers - updated to be less restrictive for Swagger UI
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

                // Modified CSP to allow Swagger UI to function properly
                if (context.Request.Path.StartsWithSegments("/swagger"))
                {
                    context.Response.Headers.Append(
                        "Content-Security-Policy",
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self'; " +
                        "connect-src 'self'; " +
                        "frame-src 'self'; " +
                        "object-src 'none'");
                }
                else
                {
                    context.Response.Headers.Append(
                        "Content-Security-Policy",
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' https://apis.google.com; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self'; " +
                        "connect-src 'self' https://apis.google.com https://login.microsoftonline.com; " +
                        "frame-src 'self' https://accounts.google.com https://login.microsoftonline.com; " +
                        "object-src 'none'");
                }

                await next();
            });

            app.MapHealthChecks("/health");

            app.UseHttpsRedirection();
            app.UseCors();

            // Always use rate limiter since we've properly configured it now
            app.UseRateLimiter();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always,
                HttpOnly = HttpOnlyPolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Apply rate limiting to specific endpoints
            var authGroup = app.MapGroup("/auth");
            authGroup.MapControllers().RequireRateLimiting("AuthEndpoints");

            // Map other controllers without special rate limiting
            app.MapControllers();
        }
    }

    public class DbConnectionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;

        public DbConnectionHealthCheck(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<EquipmentManagementContext>();
                optionsBuilder.UseMySql(
                    _connectionString,
                    new MySqlServerVersion(new Version(8, 0, 21)));

                using var dbContext = new EquipmentManagementContext(optionsBuilder.Options);
                await dbContext.Database.OpenConnectionAsync(cancellationToken);
                await dbContext.Database.CloseConnectionAsync();

                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
}
