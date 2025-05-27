using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API_Project.Configuration
{

    public static class JwtBearerConfigurationExtensions
    {
        public static void ConfigureJwtBearerOptions(
            this JwtBearerOptions options,
            AuthenticationSettings authSettings)
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
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
