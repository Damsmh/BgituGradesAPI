using AspNetCore.Authentication.ApiKey;
using BgutuGrades.Data;
using BgutuGrades.Features;
using BgutuGrades.Hubs;
using BgutuGrades.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace BgutuGrades
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("PostgreSQL")));

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            builder.Services
                .AddRepositories()
                .AddApplicationServices();
            builder.Services.AddSignalR();
            builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
            builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
                .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(options =>
                {
                    options.KeyName = "Key";
                    options.Realm = "Student Grades API";
                    options.SuppressWWWAuthenticateHeader = false;
                    options.IgnoreAuthenticationIfAllowAnonymous = true;
                });
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("ViewOnly", policy => policy.RequireRole("Student"))
                .AddPolicy("Edit", policy => policy.RequireRole("Teacher", "Admin"))
                .AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "BGITU GRADES API", Version = "v1" });
                c.DescribeAllParametersInCamelCase();
            });
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
            }

            app.UseSwagger();
            app.MapSwagger();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
            app.MapHub<MarkHub>("/hubs/mark");
            app.MapHub<GradeHub>("/hubs/grade");

            app.MapScalarApiReference("", options =>
            {
                options.WithTitle("BGITU.GRADES API")
                        .WithTheme(ScalarTheme.Purple)
                        .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            });

            app.Run();
        }
    }
}
