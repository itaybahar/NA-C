using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using System.Threading.Tasks;

namespace API_Project.Configuration
{
    public static class AuthenticationServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind authentication settings from configuration
            var authSettings = new AuthenticationSettings
            {
                SecretKey = configuration["Authentication:SecretKey"] ?? throw new ArgumentNullException("Authentication:SecretKey"),
                Issuer = configuration["Authentication:Issuer"] ?? throw new ArgumentNullException("Authentication:Issuer"),
                Audience = configuration["Authentication:Audience"] ?? throw new ArgumentNullException("Authentication:Audience"),
                ExpirationInMinutes = int.Parse(configuration["Authentication:ExpirationInMinutes"] ?? "0")
            };

            // Register AuthenticationSettings as a singleton for DI
            services.AddSingleton(authSettings);

            // Configure authentication with JWT Bearer
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authSettings.Issuer,
                    ValidAudience = authSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SecretKey))
                };
            });
            return services;
        }

        public static void ConfigureAuthentication(WebApplicationBuilder builder)
        {
            // Avoid BuildServiceProvider() which can cause memory leaks
            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
        ?? throw new ArgumentNullException("AuthenticationSettings");

            // Configure JWT as the default for API authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
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

                // Enhanced debugging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT Authentication failed: {context.Exception?.GetType().Name}: {context.Exception?.Message}");
                        if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                        {
                            Console.WriteLine($"Key used: {authSettings.SecretKey}");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated successfully for: {context.Principal?.Identity?.Name}");
                        foreach (var claim in context.Principal?.Claims ?? Array.Empty<System.Security.Claims.Claim>())
                        {
                            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            // Add other authentication handlers with explicit scheme names
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
                // Cookie configuration
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                // Other cookie options...
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options => {
                // Google configuration
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                    ?? throw new ArgumentNullException("Authentication:Google:ClientId");
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                    ?? throw new ArgumentNullException("Authentication:Google:ClientSecret");
            })
           .AddMicrosoftAccount(options =>
            {
                // Microsoft account configuration
                options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]
                    ?? throw new ArgumentNullException("Authentication:Microsoft:ClientId");
                options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]
                    ?? throw new ArgumentNullException("Authentication:Microsoft:ClientSecret");
            })
            .AddOpenIdConnect("AzureAD", options =>
            {
                // Azure AD configuration
                options.Authority = builder.Configuration["Authentication:AzureAd:Authority"];
                options.ClientId = builder.Configuration["Authentication:AzureAd:ClientId"]
                    ?? throw new ArgumentNullException("Authentication:AzureAd:ClientId");
                options.ClientSecret = builder.Configuration["Authentication:AzureAd:ClientSecret"]
                    ?? throw new ArgumentNullException("Authentication:AzureAd:ClientSecret");
                options.ResponseType = "code";
            })
            .AddJwtBearer(options =>
            {
                // Enable events for detailed logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("JWT Token received");
                        return Task.CompletedTask;
                    }
                };

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
            });

            builder.Services.AddAuthorization(options => {
                // Authorization policies
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
            });
        }
    }
}
