using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCore.Authentication.ApiKey;
using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Features;
using BgituGrades.Features.Filters;
using BgituGrades.Hubs;
using BgituGrades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using System.Text.Json.Serialization;

namespace BgituGrades
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ExcelPackage.License.SetNonCommercialOrganization("BGITU");
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("PostgreSQL")));
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")),
                    ServiceLifetime.Scoped);
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");


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

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services
                .AddRepositories()
                .AddApplicationServices()
                .AddApplicationValidation();

            builder.Services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            })
            .AddStackExchangeRedis(redisConnectionString!);

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "BgituGrades_";
            });

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
                options.AssemblyMarkerTypes = [typeof(GradeHub), typeof(ReportHub)];
                options.AsyncApi = new AsyncApiDocument
                {
                    Info = new Info("Bgitu Grades SignalR API", "v1")
                    {
                        Description = "Документация SignalR хаба"
                    }
                };
            });

            builder.Services.AddAuthorizationBuilder()
            .AddPolicy("ViewOnly", policy =>
            {
                policy.RequireRole("STUDENT", "TEACHER", "ADMIN");
                policy.AddRequirements(new GroupAccessRequirement());
            })
            .AddPolicy("Edit", policy => policy.RequireRole("TEACHER", "ADMIN"))
            .AddPolicy("Admin", policy => policy.RequireRole("ADMIN"));

            builder.Services.AddControllers(
                options =>
                {
                    options.Filters.Add<ValidationFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(2, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'V";
                    options.SubstituteApiVersionInUrl = true;
                });

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            var tokenSource = new CancellationTokenSource();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();

                    var keyService = services.GetRequiredService<IKeyService>();
                    var existingKeys = await keyService.GetKeysAsync(cancellationToken: tokenSource.Token);

                    if (!existingKeys.Any())
                    {
                        var key = await keyService.GenerateKeyAsync(Role.ADMIN, groupId: null, cancellationToken: tokenSource.Token);
                        Console.WriteLine($"##################################");
                        Console.WriteLine($"### INITIAL KEY: {key.Key} ###");
                        Console.WriteLine($"##################################");

                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Ошибка при миграции или создании начального ключа.");
                }
            }

            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
                            $"BGITU.GRADES API {description.GroupName} {(description.IsDeprecated ? "(deprecated)" : "")}"
                        );
                    }

                    options.RoutePrefix = "";
                });

                app.MapAsyncApiDocuments();
            }


            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
            app.MapHub<GradeHub>("/hubs/grade");
            app.MapHub<ReportHub>("/hubs/report");

            app.Run();
        }
    }
}
