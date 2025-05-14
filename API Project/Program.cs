using API_Project.Configuration;
using API_Project.Data;
using API_Project.Repositories;
using API_Project.Services;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Blazor_WebAssembly.Services;
namespace API_Project
{
    public class Program
    {
        // Track active ports for server-client coordination
        private static int _activeHttpPort;
        private static int _activeHttpsPort;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging before any other services
            ConfigureLogging(builder);

            // Configure Kestrel with improved certificate handling and dynamic port assignment
            ConfigureKestrel(builder);

            // Configure application services
            ConfigureServices(builder);

            // Build the application
            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);
        }

        private static void ConfigureKestrel(WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Use ConfigureEndpointDefaults to set up default HTTPS for all endpoints
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    listenOptions.UseHttps();
                });

                // Try primary ports first, but use fallbacks if they're in use
                _activeHttpPort = ConfigurePortWithFallback(options, 5191, 6000, 6100, false); // HTTP endpoint
                _activeHttpsPort = ConfigurePortWithFallback(options, 7235, 7500, 7600, true); // HTTPS endpoint

                // Store the active ports in configuration for client access
                builder.Configuration["ActiveHttpPort"] = _activeHttpPort.ToString();
                builder.Configuration["ActiveHttpsPort"] = _activeHttpsPort.ToString();

                // Log the successfully bound ports for visibility
                Console.WriteLine($"Server configured to listen on ports: HTTP: {_activeHttpPort}, HTTPS: {_activeHttpsPort}");
            });
        }

        private static int ConfigurePortWithFallback(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options,
                                                   int preferredPort, int fallbackStart, int fallbackEnd, bool useHttps)
        {
            bool portBound = false;
            int currentPort = preferredPort;

            // Try the preferred port first
            try
            {
                ConfigurePort(options, currentPort, useHttps);
                portBound = true;
                Console.WriteLine($"Successfully bound to {(useHttps ? "HTTPS" : "HTTP")} port {currentPort}");
            }
            catch (Exception ex) when (IsAddressInUseException(ex))
            {
                Console.WriteLine($"Port {currentPort} is already in use. Trying fallback ports...");
            }

            // If preferred port failed, try the fallback range
            if (!portBound)
            {
                for (currentPort = fallbackStart; currentPort <= fallbackEnd; currentPort++)
                {
                    try
                    {
                        ConfigurePort(options, currentPort, useHttps);
                        portBound = true;
                        Console.WriteLine($"Successfully bound to fallback {(useHttps ? "HTTPS" : "HTTP")} port {currentPort}");
                        break;
                    }
                    catch (Exception ex) when (IsAddressInUseException(ex))
                    {
                        // Continue to the next port
                        Console.WriteLine($"Fallback port {currentPort} is already in use. Trying next...");
                    }
                }
            }

            if (!portBound)
            {
                Console.WriteLine($"Failed to bind to any port in the range {preferredPort}, {fallbackStart}-{fallbackEnd}.");
                throw new InvalidOperationException($"Could not find an available {(useHttps ? "HTTPS" : "HTTP")} port");
            }

            // Return the successfully bound port
            return currentPort;
        }

        private static void ConfigurePort(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options, int port, bool useHttps)
        {
            options.ListenAnyIP(port, listenOptions =>
            {
                if (useHttps)
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        var environment = options.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

                        if (environment.IsDevelopment())
                        {
                            var possibleLocations = new[]
                            {
                                new { CertPath = Path.Combine(Directory.GetCurrentDirectory(), "localhost.pem"),
                                    KeyPath = Path.Combine(Directory.GetCurrentDirectory(), "localhost-key.pem") },
                                new { CertPath = Path.Combine(AppContext.BaseDirectory, "localhost.pem"),
                                    KeyPath = Path.Combine(AppContext.BaseDirectory, "localhost-key.pem") }
                            };

                            bool certificateFound = false;

                            foreach (var location in possibleLocations)
                            {
                                if (File.Exists(location.CertPath) && File.Exists(location.KeyPath))
                                {
                                    try
                                    {
                                        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(
                                            location.CertPath, location.KeyPath);
                                        certificateFound = true;
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error loading certificates: {ex.Message}");
                                    }
                                }
                            }

                            if (!certificateFound)
                            {
                                Console.WriteLine("No certificate files found. Using development certificate instead.");
                            }
                        }
                    });
                }
            });
        }

        private static bool IsAddressInUseException(Exception ex)
        {
            // Check if the exception is related to address already in use
            return ex is System.IO.IOException && ex.Message.Contains("address already in use");
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Configure email services
            ConfigureEmailService(builder);

            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();
            if (authSettings == null)
            {
                throw new InvalidOperationException("AuthenticationSettings section is missing or misconfigured in appsettings.json.");
            }
            builder.Services.AddSingleton(authSettings);

            // Configure controllers with conventions to fix route ambiguity
            builder.Services.AddControllers(options =>
            {
                // Add a convention to differentiate between controllers with similar routes
                //options.Conventions.Add(new ControllerNamePrefixConvention());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                // Add property name policy to ensure consistency between client and server
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                // Add converter for enums
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // Ensure proper number handling
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            });

            ConfigureDatabase(builder);
            RegisterNamedHttpClients(builder);
            RegisterServices(builder);

            builder.Services.AddMemoryCache();
            ConfigureRateLimiting(builder);
            ConfigureHealthChecks(builder);
            ConfigureSwagger(builder);
            ConfigureCors(builder);

            // Add authentication with JWT as the default scheme for APIs
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                if (string.IsNullOrEmpty(authSettings.SecretKey))
                {
                    throw new InvalidOperationException("SecretKey is missing in AuthenticationSettings.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(authSettings.SecretKey))
                };

                // Add debugging
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("JWT token received");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Challenge occurred: {context.Error}");
                        return Task.CompletedTask;
                    }
                };
            });

            // Add port information endpoints
            builder.Services.AddSingleton<IServerPortProvider>(new ServerPortProvider(_activeHttpPort, _activeHttpsPort));
        }

        private static void ConfigureEmailService(WebApplicationBuilder builder)
        {
            // Ensure email settings are properly configured
            var emailSection = builder.Configuration.GetSection("Email");
            if (!emailSection.Exists())
            {
                Console.WriteLine("WARNING: Email configuration section is missing. Email functionality may not work correctly.");
            }
            else
            {
                var smtpHost = emailSection["Smtp:Host"];
                var smtpPort = emailSection["Smtp:Port"];
                var smtpUser = emailSection["Smtp:Username"];
                var smtpPass = emailSection["Smtp:Password"];
                var fromEmail = emailSection["From"];

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPort) ||
                    string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass) ||
                    string.IsNullOrEmpty(fromEmail))
                {
                    Console.WriteLine("WARNING: One or more email configuration values are missing. Email functionality may not work correctly.");
                }
                else
                {
                    Console.WriteLine($"Email configuration detected. SMTP Host: {smtpHost}, From: {fromEmail}");
                }
            }

            // Register the email service
            builder.Services.AddScoped<IEmailService, EmailService>();
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured in appsettings.json.");
            }

            builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    )
                )
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
            );
        }

        private static void RegisterNamedHttpClients(WebApplicationBuilder builder)
        {
            // Use the dynamic HTTPS port for API base address
            var apiBaseAddress = $"https://localhost:{_activeHttpsPort}";
            builder.Configuration["API_BaseAddress"] = apiBaseAddress;

            builder.Services.AddHttpClient("API", client =>
            {
                client.BaseAddress = new Uri(apiBaseAddress);
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management API Client");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            builder.Services.AddHttpClient("Auth", client =>
            {
                client.BaseAddress = new Uri(apiBaseAddress);
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management Auth Client");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            builder.Services.AddHttpClient("External", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management External Client");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleChangeRequestRepository, RoleChangeRequestRepository>();
            builder.Services.AddScoped<ITeamRepository, TeamRepository>();
            builder.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();
            builder.Services.AddScoped<IBlacklistRepository, BlacklistRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWorkRepository>();

            builder.Services.AddScoped<Services.AuthenticationService>();
            builder.Services.AddScoped<Domain_Project.Interfaces.IAuthenticationService>(sp =>
                new AuthenticationServiceAdapter(
                    sp.GetRequiredService<Services.AuthenticationService>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Auth")));

            builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IBlacklistService, BlacklistService>();

            builder.Services.AddScoped<IEquipmentService>(sp =>
                new EquipmentService(
                    sp.GetRequiredService<IEquipmentRepository>(),
                    sp.GetRequiredService<EquipmentManagementContext>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"),
                    sp.GetRequiredService<ILogger<EquipmentService>>())
                {
                    JsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        ReferenceHandler = ReferenceHandler.Preserve,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    }
                });

            // Fix issue with CheckoutService
            builder.Services.AddScoped<ICheckoutService, API_Project.Services.CheckoutService>(sp =>
                new API_Project.Services.CheckoutService(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API")));
        }

        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            builder.Services.AddRateLimiter(options =>
            {
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

                options.AddPolicy("AuthEndpoints", context =>
                {
                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // Special policy for services that might make many API calls
                options.AddPolicy("ServiceEndpoints", context =>
                {
                    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(clientIp,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 300, // Much higher limit for service endpoints
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.Headers.Append("Retry-After", "60");
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

            // Add email health check
            builder.Services.AddHealthChecks()
                .AddCheck(
                    "email-config",
                    new EmailConfigHealthCheck(builder.Configuration),
                    tags: new[] { "email" },
                    timeout: TimeSpan.FromSeconds(5)
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
            // Include all possible port combinations for client (Blazor WebAssembly) connectivity
            var allowedOrigins = new List<string>
            {
                // Basic localhost origins
                "https://localhost:5001", "http://localhost:5000",
                "https://localhost:7176", "http://localhost:5176",
                "https://localhost:7177", "http://localhost:5177",
                "https://localhost:7178", "http://localhost:5178",
                "https://localhost:7179", "http://localhost:5179",
                "https://localhost:7180", "http://localhost:5180",
                
                // Additional origins for VS2022 and other tools
                "https://localhost:44300", "http://localhost:44300",
                "https://localhost:44301", "http://localhost:44301",
                "https://localhost:44302", "http://localhost:44302",
                "https://localhost:44303", "http://localhost:44303",
                
                // Common .NET dev ports
                "https://localhost:5000", "http://localhost:5000",
                "https://localhost:5001", "http://localhost:5001",
                "https://localhost:5002", "http://localhost:5002",
                "https://localhost:5003", "http://localhost:5003",
                "https://localhost:5004", "http://localhost:5004",
                "https://localhost:5005", "http://localhost:5005",
                "https://localhost:5010", "http://localhost:5010",
                "https://localhost:5011", "http://localhost:5011",
                "https://localhost:5012", "http://localhost:5012",
                "https://localhost:5013", "http://localhost:5013",
                "https://localhost:5014", "http://localhost:5014",
                "https://localhost:5015", "http://localhost:5015",
                
                // Common ranges used by Visual Studio
                "https://localhost:7000", "http://localhost:7000",
                "https://localhost:7001", "http://localhost:7001",
                "https://localhost:7002", "http://localhost:7002",
                "https://localhost:7003", "http://localhost:7003",
                
                // IIS Express ranges
                "https://localhost:44300", "http://localhost:44300",
                "https://localhost:44301", "http://localhost:44301",
                "https://localhost:44302", "http://localhost:44302",
            };

            // Add ports from config if available
            var configOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
            if (configOrigins != null)
            {
                allowedOrigins.AddRange(configOrigins);
            }

            // Add dynamic origins based on active ports
            allowedOrigins.Add($"https://localhost:{_activeHttpsPort}");
            allowedOrigins.Add($"http://localhost:{_activeHttpPort}");

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });

                options.AddPolicy("AllowAll", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true) // Allow any origin
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Add global exception handling middleware
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var logger = app.Services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(contextFeature.Error, "Unhandled exception occurred");

                        await context.Response.WriteAsJsonAsync(new
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = app.Environment.IsDevelopment()
                                ? $"Error: {contextFeature.Error.Message}"
                                : "An unexpected error occurred. Please try again later."
                        });
                    }
                });
            });

            // Development-specific middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Management API v1");
                    c.RoutePrefix = "swagger";
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                    c.EnableDeepLinking();
                    c.DisplayRequestDuration();
                });

                app.MapGet("/", context =>
                {
                    context.Response.Redirect("/swagger");
                    return Task.CompletedTask;
                });
            }
            else
            {
                // Already configured at the top of the method
                app.UseHsts();
            }

            // CORS configuration - IMPORTANT: Configure CORS before other middleware
            app.UseCors("AllowAll"); // Apply the permissive CORS policy for development
            app.UseCors(); // Apply the default policy

            // Health check endpoints with improved response
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var result = new
                    {
                        Status = report.Status.ToString(),
                        Duration = report.TotalDuration,
                        Checks = report.Entries.Select(e => new
                        {
                            Component = e.Key,
                            Status = e.Value.Status.ToString(),
                            Description = e.Value.Description ?? string.Empty,
                            Duration = e.Value.Duration
                        })
                    };

                    await context.Response.WriteAsJsonAsync(result);
                }
            });

            // API version and status endpoint
            app.MapGet("/api/status", () => new
            {
                Status = "Running",
                Version = "1.0.0",
                Environment = app.Environment.EnvironmentName,
                Timestamp = DateTimeOffset.UtcNow
            });

            // Add port discovery endpoint for clients
            app.MapGet("/api/server-info/ports", (IServerPortProvider portProvider) =>
                new { HttpPort = portProvider.HttpPort, HttpsPort = portProvider.HttpsPort });

            // Add CORS preflight handling middleware for all OPTIONS requests - FIX: Use Append for headers
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization");
                    context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    context.Response.StatusCode = 200;
                    return;
                }

                await next();
            });

            // Add email test endpoint - FIX: Using nullable dictionary
            app.MapGet("/api/email/test", async (IEmailService emailService, IConfiguration config) =>
            {
                try
                {
                    var testEmail = config["Email:TestEmail"] ?? "test@example.com";
                    await emailService.SendEmailAsync(
                        testEmail,
                        "Equipment Management System - Test Email",
                        "<h1>Email Test</h1><p>This is a test email from the Equipment Management System API.</p>");
                    return Results.Ok(new { Success = true, Message = "Test email sent successfully" });
                }
                catch (Exception ex)
                {
                    // Create dictionary with nullable values to match expected type
                    var extensions = new Dictionary<string, object?> { { "TraceId", Guid.NewGuid().ToString() } };

                    return Results.Problem(
                        title: "Email Test Failed",
                        detail: $"Failed to send test email: {ex.Message}",
                        statusCode: 500,
                        extensions: extensions
                    );
                }
            })
            .AllowAnonymous(); // Allow anonymous access during development

            // Standard middleware pipeline
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseRateLimiter();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always,
                HttpOnly = HttpOnlyPolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Detailed request/response logging for troubleshooting
            if (app.Environment.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();

                    // Log request details
                    logger.LogInformation(
                        "Request: {Method} {Path}{QueryString} | Client IP: {IP}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.QueryString,
                        context.Connection.RemoteIpAddress);

                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        logger.LogDebug("Auth header received: {AuthHeader}", authHeader.ToString().Substring(0, Math.Min(15, authHeader.ToString().Length)) + "...");
                    }

                    var originalBodyStream = context.Response.Body;
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        context.Response.Body = memoryStream;

                        // Execute the request pipeline
                        await next();

                        // Log the response
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        logger.LogInformation(
                            "Response: {StatusCode} for {Method} {Path}",
                            context.Response.StatusCode,
                            context.Request.Method,
                            context.Request.Path);

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(originalBodyStream);
                    }
                    finally
                    {
                        context.Response.Body = originalBodyStream;
                    }
                });
            }
            else
            {
                // Simple logging middleware for production
                app.Use(async (context, next) =>
                {
                    try
                    {
                        await next();

                        // Log errors for non-success status codes
                        if (context.Response.StatusCode >= 400)
                        {
                            var logger = app.Services.GetRequiredService<ILogger<Program>>();
                            logger.LogWarning(
                                "Error response: {StatusCode} for {Method} {Path}",
                                context.Response.StatusCode,
                                context.Request.Method,
                                context.Request.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = app.Services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "Unhandled exception in request pipeline");
                        throw; // Re-throw to be handled by the exception handler middleware
                    }
                });
            }

            // Endpoints for detecting API availability from client
            app.MapGet("/api/ping", () => new {
                Timestamp = DateTime.UtcNow,
                Status = "OK",
                Message = "API is available"
            });

            app.MapGet("/api/test", () => Results.Ok(new
            {
                Status = "Success",
                Message = "API test endpoint working"
            }));

            // Configure API route groups
            var authGroup = app.MapGroup("/auth");
            authGroup.MapControllers()
                .AllowAnonymous(); // Allow anonymous access to auth endpoints during development

            var equipmentCheckoutGroup = app.MapGroup("/api/equipmentcheckout");
            equipmentCheckoutGroup.MapControllers()
                .AllowAnonymous(); // Allow anonymous access during development

            // Regular controllers
            app.MapControllers();

            // Ensure database is ready
            EnsureDatabaseConnectionAsync(app).GetAwaiter().GetResult();

            var addresses = app.Urls.Select(url => new Uri(url));
            Console.WriteLine("Server running at: " + string.Join(", ", addresses.Select(addr => addr.ToString())));

            app.Run();
        }

        private static async Task EnsureDatabaseConnectionAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EquipmentManagementContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Testing database connection...");
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    logger.LogCritical("Cannot connect to the database!");
                    throw new ApplicationException("Database connection failed");
                }

                var equipmentCount = await dbContext.Equipment.CountAsync();
                logger.LogInformation("Database connection successful. Equipment count: {Count}", equipmentCount);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database connection error: {Message}", ex.Message);
                throw;
            }
        }
    }

    // Convention to prefix controller routes to avoid ambiguity between similarly named controllers
    public class ControllerNamePrefixConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Check if controller name contains "User" as these are the conflicting controllers
            if (controller.ControllerName.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                // Add a prefix to all routes in the controller
                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel == null)
                    {
                        selector.AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = $"api/single-users/[controller]"
                        };
                    }
                    else
                    {
                        // If the route is already explicitly set, we'll prepend our prefix
                        var template = selector.AttributeRouteModel.Template;
                        if (!string.IsNullOrEmpty(template))
                        {
                            selector.AttributeRouteModel.Template = $"api/single-users/{template}";
                        }
                    }
                }
            }
            else if (controller.ControllerName.Equals("Users", StringComparison.OrdinalIgnoreCase))
            {
                // Add a different prefix to distinguish from UserController
                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel == null)
                    {
                        selector.AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = $"api/multiple-users/[controller]"
                        };
                    }
                    else
                    {
                        var template = selector.AttributeRouteModel.Template;
                        if (!string.IsNullOrEmpty(template))
                        {
                            selector.AttributeRouteModel.Template = $"api/multiple-users/{template}";
                        }
                    }
                }
            }
        }
    }

    // Interface for providing port information
    public interface IServerPortProvider
    {
        int HttpPort { get; }
        int HttpsPort { get; }
    }

    // Implementation of port provider
    public class ServerPortProvider : IServerPortProvider
    {
        public int HttpPort { get; }
        public int HttpsPort { get; }

        public ServerPortProvider(int httpPort, int httpsPort)
        {
            HttpPort = httpPort;
            HttpsPort = httpsPort;
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

    public class EmailConfigHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public EmailConfigHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var smtpHost = _configuration["Email:Smtp:Host"];
                var smtpPortString = _configuration["Email:Smtp:Port"];
                var smtpUser = _configuration["Email:Smtp:Username"];
                var smtpPass = _configuration["Email:Smtp:Password"];
                var fromEmail = _configuration["Email:From"];

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPortString) ||
                    string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass) ||
                    string.IsNullOrEmpty(fromEmail))
                {
                    return Task.FromResult(
                        HealthCheckResult.Degraded("Email configuration is incomplete"));
                }

                if (!int.TryParse(smtpPortString, out _))
                {
                    return Task.FromResult(
                        HealthCheckResult.Degraded("SMTP port configuration is invalid"));
                }

                return Task.FromResult(HealthCheckResult.Healthy("Email configuration is valid"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Error checking email configuration", ex));
            }
        }
    }
}
