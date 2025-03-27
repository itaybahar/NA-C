using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.More;
using System.Text;

// Your project-specific namespaces
using API_Project.Data;
using Domain_Project.Interfaces;
using API_Project.Repositories;
using API_Project.Services;
using API_Project.Configuration;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add controllers
            services.AddControllers();

            // Configure DbContext
            services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 21))
                )
            );

            // Configure Authentication Settings
            var authSettings = new AuthenticationSettings
            {
                SecretKey = configuration["Authentication:SecretKey"],
                Issuer = configuration["Authentication:Issuer"],
                Audience = configuration["Authentication:Audience"],
                ExpirationInMinutes = int.Parse(configuration["Authentication:ExpirationInMinutes"] ?? "60")
            };
            services.AddSingleton(authSettings);

            // Configure Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();

            // Configure Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Configure Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Configure Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        IssuerSigningKey = authSettings.GetSymmetricSecurityKey()
                    };
                });

            // Configure Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("WarehouseManager", policy =>
                    policy.RequireRole("WarehouseManager"));
                options.AddPolicy("CentralManager", policy =>
                    policy.RequireRole("CentralManager"));
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}