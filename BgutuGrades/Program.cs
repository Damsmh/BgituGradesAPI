using BgutuGrades.Data;
using BgutuGrades.Features;
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
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                }); ;
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
            app.UseAuthorization();

            app.MapControllers();

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
