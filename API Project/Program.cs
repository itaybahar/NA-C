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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    // Use X509Certificate2.CreateFromPemFile instead of constructor
                    var certPath = "./localhost.pem";
                    var keyPath = "./localhost-key.pem";
                    httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                });
            });

            ConfigureServices(builder);
            var app = builder.Build();

            ConfigurePipeline(app);

            app.MapGet("/", () => "Welcome to Equipment Management API");

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Add JSON options to handle cycles and maintain reference integrity
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Configure DbContext with improved connection resilience
            builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mySqlOptions => mySqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                )
            );

            // Register HttpClient for Google authentication
            builder.Services.AddHttpClient();

            // Configure Authentication Settings
            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
                ?? throw new ArgumentNullException("AuthenticationSettings");
            builder.Services.AddSingleton(authSettings);

            // Configure Repositories
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleChangeRequestRepository, RoleChangeRequestRepository>();

            // Register the AuthenticationService first
            builder.Services.AddScoped<Services.AuthenticationService>();

            // Configure Authentication Service
            builder.Services.AddScoped<Domain_Project.Interfaces.IAuthenticationService, AuthenticationServiceAdapter>();

            // Register Role Services - Use API project's implementation, not the Blazor WebAssembly one
            builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();

            // Remove this line or replace with server-side implementation
            // builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IRoleService, Blazor_WebAssembly.Services.RoleService>();

            // Register IEmailService
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Add memory caching for performance
            builder.Services.AddMemoryCache();

            // Add health checks with database check
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddHealthChecks()
                .AddCheck(
                    "mysql-connection",
                    () =>
                    {
                        try
                        {
                            // Create a scope to resolve the DbContext
                            using var serviceScope = builder.Services.BuildServiceProvider().CreateScope();
                            var dbContext = serviceScope.ServiceProvider.GetRequiredService<EquipmentManagementContext>();

                            // Try to connect to the database
                            dbContext.Database.OpenConnection();
                            dbContext.Database.CloseConnection();

                            return HealthCheckResult.Healthy("Database connection is healthy");
                        }
                        catch (Exception ex)
                        {
                            return HealthCheckResult.Unhealthy("Database connection failed", ex);
                        }
                    },
                    tags: new[] { "db", "mysql" },
                    timeout: TimeSpan.FromSeconds(30)
                );

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Equipment Management API",
                    Version = "v1",
                    Description = "API for Equipment Management System",
                    Contact = new OpenApiContact
                    {
                        Name = "API Support"
                    }
                });

                // Add JWT Authentication support in Swagger UI
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

                // Add this to resolve ambiguous type references
                c.CustomSchemaIds(type => type.FullName);
            });

            // CORS - Updated to allow credentials and configured origins
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                new[] { "https://localhost:7176" }; // Blazor WASM origin

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Google + JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None; // Required for CORS
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:GoogleClientId"]
                    ?? throw new ArgumentNullException("GoogleClientId");
                options.ClientSecret = builder.Configuration["Authentication:GoogleClientSecret"]
                    ?? throw new ArgumentNullException("GoogleClientSecret");
                options.CallbackPath = "/auth/google-callback";
                options.SaveTokens = true;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authSettings.Issuer,
                    ValidAudience = authSettings.Audience,
                    IssuerSigningKey = authSettings.GetSymmetricSecurityKey(),
                    ClockSkew = TimeSpan.Zero // Reduce the default 5 minute tolerance for token expiration
                };

                // Enable using JWT tokens in WebSocket connections
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add custom authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("WarehouseManagementOnly", policy =>
                    policy.RequireRole("Admin", "WarehouseManager", "WarehouseOperator"));
                options.AddPolicy("AdminOrWarehouseManager", policy =>
                    policy.RequireRole("Admin", "WarehouseManager"));
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Enable Swagger only in development
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Management API v1");
                    c.RoutePrefix = string.Empty;
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                });
            }
            else
            {
                // Add production error handling
                app.UseExceptionHandler("/Error");
                app.UseHsts(); // HTTP Strict Transport Security
            }

            // Add health check endpoint
            app.MapHealthChecks("/health");

            app.UseHttpsRedirection();
            app.UseCors();

            // Add security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
        }
    }

    /// <summary>
    /// Adapter that bridges between API_Project's AuthenticationService and Domain_Project's IAuthenticationService
    /// </summary>
    public class AuthenticationServiceAdapter : Domain_Project.Interfaces.IAuthenticationService
    {
        private readonly Services.AuthenticationService _authService;
        private readonly IUserRepository _userRepository;

        public AuthenticationServiceAdapter(Services.AuthenticationService authService, IUserRepository userRepository)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public string GenerateJwtToken(User user)
        {
            return _authService.GenerateJwtToken(user);
        }

        public async Task RegisterUserAsync(UserDto userDto, string password)
        {
            await _authService.RegisterUserAsync(userDto, password);
        }

        public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateAsync(UserLoginDto loginDto)
        {
            var result = await _authService.AuthenticateAsync(loginDto);
            return ConvertToAuthResponseDto(result);
        }

        public async Task<Domain_Project.DTOs.AuthenticationResponseDto> AuthenticateWithGoogleAsync(string googleToken)
        {
            var result = await _authService.AuthenticateWithGoogleAsync(googleToken);
            return ConvertToAuthResponseDto(result);
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            await _authService.SendPasswordResetEmailAsync(email);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            return await _authService.ResetPasswordAsync(token, newPassword);
        }

        private static Domain_Project.DTOs.AuthenticationResponseDto ConvertToAuthResponseDto(API_Project.Services.AuthenticationResponseDto source)
        {
            return new Domain_Project.DTOs.AuthenticationResponseDto
            {
                Token = source.Token,
                User = source.User
            };
        }

        public bool ValidatePassword(User user, string password)
        {
            return _userRepository.ValidateUserCredentialsAsync(user.Username, password).GetAwaiter().GetResult();
        }
    }
}
