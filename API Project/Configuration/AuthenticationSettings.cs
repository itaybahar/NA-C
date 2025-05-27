using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API_Project.Configuration
{   
     public class AuthenticationSettings
    {
        public required string SecretKey { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public int ExpirationInMinutes { get; set; } = 60;

        public SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        }
    }
} 