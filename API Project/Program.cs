using API_Project.Configuration;
using API_Project.Data;
using API_Project.Repositories;
using API_Project.Services;
using Domain_Project.DTOs;
using Domain_Project.Interfaces;
using Domain_Project.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging before any other services
            ConfigureLogging(builder);

            // Configure Kestrel with improved certificate handling
            ConfigureKestrel(builder);

            // Configure application services
            ConfigureServices(builder);

            // Build the application (only once)
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

                            bool certificateFound = false;

                            foreach (var location in possibleLocations)
                            {
                                if (File.Exists(location.CertPath) && File.Exists(location.KeyPath))
                                {
                                    try
                                    {
                                        httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(location.CertPath, location.KeyPath);
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

                options.ListenAnyIP(7235, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            });
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();
            if (authSettings == null)
            {
                throw new InvalidOperationException("AuthenticationSettings section is missing or misconfigured in appsettings.json.");
            }
            builder.Services.AddSingleton(authSettings);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
                    ValidIssuer = authSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authSettings.Audience,
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
            builder.Services.AddHttpClient("API", client =>
            {
                var baseAddress = builder.Configuration["API_BaseAddress"] ?? "https://localhost:7235";
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management API Client");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            builder.Services.AddHttpClient("Auth", client =>
            {
                client.BaseAddress = new Uri("https://localhost:5191/");
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management Auth Client");
            });

            builder.Services.AddHttpClient("External", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Equipment Management External Client");
            });
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            // Repository registrations
            builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleChangeRequestRepository, RoleChangeRequestRepository>();
            builder.Services.AddScoped<ITeamRepository, TeamRepository>();
            builder.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();
            builder.Services.AddScoped<IBlacklistRepository, BlacklistRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWorkRepository>();

            // Service registrations with proper logger injection
            builder.Services.AddScoped<Services.AuthenticationService>();
            builder.Services.AddScoped<Domain_Project.Interfaces.IAuthenticationService>(sp =>
                new AuthenticationServiceAdapter(
                    sp.GetRequiredService<Services.AuthenticationService>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Auth")));

            builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IBlacklistService, BlacklistService>();

            // Update the EquipmentService registration in RegisterServices method
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
                        ReferenceHandler = ReferenceHandler.Preserve
                    }
                });
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
            // Define allowed origins explicitly including your Blazor WebAssembly app's origin
            var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ??
                new[] { "https://localhost:7176", "http://localhost:5176" };

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials(); // Important for authentication
                });
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Development-specific configurations  
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
                // Production-specific configurations  
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Middleware for request handling  
            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Serve static files if needed  
            app.UseRouting();

            // CORS configuration  
            app.UseCors();
            app.UseCors("AllowAll");
            // Rate limiting to prevent abuse  
            app.UseRateLimiter();

            // Cookie policy for secure handling  
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always,
                HttpOnly = HttpOnlyPolicy.Always
            });

            // Authentication and Authorization  
            app.UseAuthentication();
            app.UseAuthorization();

            // Custom logging for requests and responses  
            // Custom logging for requests and responses  
            app.Use(async (context, next) =>
            {
                try
                {
                    // Print authorization header  
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    {
                        Console.WriteLine($"Auth header received: {authHeader}");
                    }
                    else
                    {
                        Console.WriteLine("No Authorization header present");
                    }

                    // Print authentication status before processing with additional null checks
                    Console.WriteLine($"Request path: {context.Request.Path}");
                    Console.WriteLine($"User authenticated (pre): {(context.User?.Identity != null ? context.User.Identity.IsAuthenticated : "Identity is null")}");

                    await next();

                    // Print authentication status after processing with additional null checks
                    Console.WriteLine($"User authenticated (post): {(context.User?.Identity != null ? context.User.Identity.IsAuthenticated : "Identity is null")}");
                    Console.WriteLine($"Response status code: {context.Response.StatusCode}");
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur in the middleware
                    Console.WriteLine($"Error in auth logging middleware: {ex.Message}");
                    await next(); // Make sure the pipeline continues even if our logging fails
                }
            });


            // Map grouped endpoints for authentication  
            var authGroup = app.MapGroup("/auth");
            authGroup.MapControllers().RequireRateLimiting("AuthEndpoints");

            // Map all other controllers  
            app.MapControllers();

            // Health check endpoint  
            app.MapHealthChecks("/health");

            // Ensure database connection before running the app  
            EnsureDatabaseConnectionAsync(app).GetAwaiter().GetResult();

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
                // Test simple query
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    logger.LogCritical("Cannot connect to the database!");
                    throw new ApplicationException("Database connection failed");
                }

                // Test Equipment table access
                var equipmentCount = await dbContext.Equipment.CountAsync();
                logger.LogInformation("Database connection successful. Equipment count: {Count}", equipmentCount);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database connection error: {Message}", ex.Message);
                throw; // Fail fast if database is not accessible
            }
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
