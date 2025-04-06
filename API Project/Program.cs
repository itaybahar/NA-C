using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Project-specific namespaces
using API_Project.Data;
using API_Project.Repositories;
using API_Project.Services;
using Domain_Project.Interfaces;
using API_Project.Configuration;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            // Add a default route
            app.MapGet("/", () => "Welcome to Equipment Management API");

            app.Run();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            // Add core services
            builder.Services.AddControllers();

            // Configure DbContext
            builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 21))
                )
            );

            // Configure Authentication Settings
            var authSettings = new Configuration.AuthenticationSettings
            {
                SecretKey = builder.Configuration["Jwt:Key"],
                Issuer = builder.Configuration["Jwt:Issuer"],
                Audience = builder.Configuration["Jwt:Audience"],
                ExpirationInMinutes = int.Parse(builder.Configuration["Jwt:ExpirationInMinutes"] ?? "60")
            };
            builder.Services.AddSingleton(authSettings);

            // Configure Repositories
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Configure Authentication Service
            builder.Services.AddScoped<Services.IAuthenticationService, API_Project.Services.AuthenticationService>();

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Equipment Management API",
                    Version = "v1"
                });
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Configure Authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IServiceCollection serviceCollection = builder.Services.AddAuthorization();
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Enable detailed error pages in development
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Management API v1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
            });

            // Enforce HTTPS
            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors();

            // Add authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers
            app.MapControllers();
        }
    }
}