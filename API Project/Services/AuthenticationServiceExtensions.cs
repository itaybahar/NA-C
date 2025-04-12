using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                    ValidIssuer = authSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authSettings.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SecretKey)),
                    ValidateIssuerSigningKey = true
                };
            });
            return services;
        }

        public static void ConfigureAuthentication(WebApplicationBuilder builder)
        {
            // Avoid BuildServiceProvider() which can cause memory leaks
            var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
                ?? throw new ArgumentNullException("AuthenticationSettings");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                // Cookie authentication configuration
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.Name = "EquipmentMgmt.Auth";
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;

                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
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
                // Google authentication configuration
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
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // JWT Bearer configuration
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authSettings.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.SecretKey)),
                    ValidateIssuerSigningKey = true
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                // Authorization policies
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserPolicy", policy => policy.RequireRole("User"));
            });
        }
    }
}
