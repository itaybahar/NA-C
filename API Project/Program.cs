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
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
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
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null))
            );

            builder.Services.AddHttpClient();

            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
                ?? throw new ArgumentNullException("AuthenticationSettings");
            builder.Services.AddSingleton(authSettings);

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleChangeRequestRepository, RoleChangeRequestRepository>();
            builder.Services.AddScoped<Services.AuthenticationService>();
            builder.Services.AddScoped<Domain_Project.Interfaces.IAuthenticationService, AuthenticationServiceAdapter>();
            builder.Services.AddScoped<IRoleRequestService, RoleRequestService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddMemoryCache();

            // Add rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter("GlobalLimiter",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.AddPolicy("AuthEndpoints", context =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter("AuthLimiter",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });

            builder.Services.AddHealthChecks()
                .AddCheck(
                    "mysql-connection",
                    () =>
                    {
                        try
                        {
                            using var serviceScope = builder.Services.BuildServiceProvider().CreateScope();
                            var dbContext = serviceScope.ServiceProvider.GetRequiredService<EquipmentManagementContext>();
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

            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                new[] { "https://localhost:7176" };

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

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.Name = "EquipmentMgmt.Auth";
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;

                // Add CSRF protection
                options.Cookie.IsEssential = true;

                // Add event handlers for authentication events
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        // For API requests, return 401 instead of redirect
                        if (context.Request.Path.StartsWithSegments("/api") ||
                            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:GoogleClientId"]
                    ?? throw new ArgumentNullException("GoogleClientId");
                options.ClientSecret = builder.Configuration["Authentication:GoogleClientSecret"]
                    ?? throw new ArgumentNullException("GoogleClientSecret");
                options.CallbackPath = "/auth/google-callback";
                options.SaveTokens = true;
                options.AccessType = "offline"; // Request a refresh token

                options.Events.OnRemoteFailure = context =>
                {
                    Console.WriteLine("Google Login Failed: " + context.Failure?.Message);
                    context.Response.Redirect("/login?error=google");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };

                options.Events.OnTicketReceived = context =>
                {
                    // Log successful authentications
                    Console.WriteLine($"User {context.Principal?.Identity?.Name} authenticated successfully with Google");
                    return Task.CompletedTask;
                };
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
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Extract token from query string for SignalR or WebSockets
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        Console.WriteLine($"JWT authentication failed: {context.Exception?.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"JWT token validated successfully for {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // Add more descriptive error responses
                        if (context.AuthenticateFailure != null)
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var message = context.AuthenticateFailure is SecurityTokenExpiredException
                                ? "The token has expired"
                                : "Invalid authentication";
                            return context.Response.WriteAsync(
                                System.Text.Json.JsonSerializer.Serialize(new { error = message }));
                        }
                        return Task.CompletedTask;
                    }
                };

                options.SaveToken = true; // Store the token in the authentication properties
                options.RequireHttpsMetadata = true; // Require HTTPS for all requests
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("WarehouseManagementOnly", policy =>
                    policy.RequireRole("Admin", "WarehouseManager", "WarehouseOperator"));
                options.AddPolicy("AdminOrWarehouseManager", policy =>
                    policy.RequireRole("Admin", "WarehouseManager"));

                // Add a fallback policy for all endpoints
                options.FallbackPolicy = options.DefaultPolicy;
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.MapHealthChecks("/health");

            app.UseHttpsRedirection();
            app.UseCors();

            // Add rate limiting middleware
            app.UseRateLimiter();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always,
                HttpOnly = HttpOnlyPolicy.Always
            });

            // Add security headers middleware
            app.Use(async (context, next) =>
            {
                // Basic security headers
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

                // Enhanced security headers
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

                // Content Security Policy
                context.Response.Headers.Append(
                    "Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' https://apis.google.com; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self'; " +
                    "connect-src 'self' https://apis.google.com; " +
                    "frame-src 'self' https://accounts.google.com; " +
                    "object-src 'none'");

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            // Apply rate limiting specifically to auth endpoints
            app.MapControllers().RequireRateLimiting("AuthEndpoints");
        }
    }
}
