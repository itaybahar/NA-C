using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API_Project.Configuration
{
    public static class AuthenticationServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind authentication settings from configuration
            var authSettings = new AuthenticationSettings
            {
                SecretKey = configuration["Authentication:SecretKey"],
                Issuer = configuration["Authentication:Issuer"],
                Audience = configuration["Authentication:Audience"],
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
            .AddJwtBearer(options => options.ConfigureJwtBearerOptions(authSettings));

            return services;
        }
    }
}
