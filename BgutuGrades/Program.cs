using AspNetCore.Authentication.ApiKey;
using BgutuGrades.Data;
using BgutuGrades.Features;
using BgutuGrades.Hubs;
using BgutuGrades.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Saunter;
using Saunter.AsyncApiSchema.v2;
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
            builder.Services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            }); ;
            builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
            builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
                .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(options =>
                {
                    options.KeyName = "key";
                    options.Realm = "Student Grades API";
                    options.SuppressWWWAuthenticateHeader = false;
                    options.IgnoreAuthenticationIfAllowAnonymous = true;
                });
            builder.Services.AddAsyncApiSchemaGeneration(options =>
            {
                options.AssemblyMarkerTypes = [typeof(GradeHub)];
                options.AsyncApi = new AsyncApiDocument
                {
                    Info = new Info("Student Grades Real-time API", "1.0.0")
                    {
                        Description = "Документация SignalR хаба для работы с оценками и посещаемостью"
                    }
                };
            });
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("ViewOnly", policy => policy.RequireRole("STUDENT", "TEACHER", "ADMIN"))
                .AddPolicy("Edit", policy => policy.RequireRole("TEACHER", "ADMIN"))
                .AddPolicy("Admin", policy => policy.RequireRole("ADMIN"));
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
            app.MapAsyncApiDocuments();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
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
