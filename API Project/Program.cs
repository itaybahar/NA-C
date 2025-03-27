using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Your project-specific namespaces
using API_Project.Data;
using Domain_Project.Interfaces;
using API_Project.Repositories;
using API_Project.Services;

namespace API_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Configure services
                ConfigureServices(builder);

                var app = builder.Build();

                // Configure the HTTP request pipeline
                ConfigurePipeline(app);

                app.Run();
            }
            catch (Exception ex)
            {
                // Log the full exception details
                Console.WriteLine($"Startup failed: {ex}");
                throw;
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            try
            {
                // Add basic services
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();

                // Configure health checks
                builder.Services.AddHealthChecks();

                // Configure DbContext
                ConfigureDbContext(builder);

                // Configure Swagger
                ConfigureSwagger(builder);

                // Configure CORS
                ConfigureCors(builder);

                // Configure Repositories and Services
                ConfigureRepositoriesAndServices(builder);

                // Configure Authentication
                ConfigureAuthentication(builder);

                // Configure Authorization
                ConfigureAuthorization(builder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureServices: {ex}");
                throw;
            }
        }

        private static void ConfigureDbContext(WebApplicationBuilder builder)
        {
            try
            {
                builder.Services.AddDbContext<EquipmentManagementContext>(options =>
                {
                    options.UseMySql(
                        builder.Configuration.GetConnectionString("DefaultConnection"),
                        new MySqlServerVersion(new Version(8, 0, 21)),
                        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                    );
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DbContext configuration failed: {ex}");
                throw;
            }
        }

        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Equipment Management API",
                    Version = "v1"
                });

                // JWT Authentication in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
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
                        new string[] {}
                    }
                });
            });
        }

        private static void ConfigureCors(WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder
                        .WithOrigins(
                            "http://localhost:3000",
                            "https://localhost:3000",
                            "http://localhost:5000",
                            "https://localhost:5000"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                );
            });
        }

        private static void ConfigureRepositoriesAndServices(WebApplicationBuilder builder)
        {
            // Ensure all required services are registered
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        }

        private static void ConfigureAuthentication(WebApplicationBuilder builder)
        {
            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
        }

        private static void ConfigureAuthorization(WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("WarehouseManager", policy =>
                    policy.RequireRole("WarehouseManager"));
                options.AddPolicy("CentralManager", policy =>
                    policy.RequireRole("CentralManager"));
            });
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            // Enable detailed exception page in development
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // Configure Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_Project v1");
                    c.RoutePrefix = "api-docs";
                });
            }
            else
            {
                // Use exception handler in production
                app.UseExceptionHandler("/error");

                // Enable HSTS (HTTP Strict Transport Security)
                app.UseHsts();
            }

            // Enforce HTTPS redirection
            app.UseHttpsRedirection();

            // Enable CORS
            app.UseCors("AllowSpecificOrigin");

            // Enable static files (if you have any)
            app.UseStaticFiles();

            // Add authentication middleware
            app.UseAuthentication();

            // Add authorization middleware
            app.UseAuthorization();

            // Map controllers
            app.MapControllers();

            // Add health checks
            app.MapHealthChecks("/health");
        }
    }
}