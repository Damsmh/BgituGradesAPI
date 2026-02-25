using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCore.Authentication.ApiKey;
using BgituGrades.Data;
using BgituGrades.Entities;
using BgituGrades.Features;
using BgituGrades.Hubs;
using BgituGrades.Repositories;
using BgituGrades.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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

            builder.Services
                .AddRepositories()
                .AddApplicationServices();

            builder.Services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            })
            .AddStackExchangeRedis(redisConnectionString);

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
                        Description = "ƒокументаци€ SignalR хаба"
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

            builder.Services.AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();

                var provider = builder.Services.BuildServiceProvider()
                    .GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerDoc(description.GroupName, new OpenApiInfo
                    {
                        Title = $"BGITU.GRADES API",
                        Version = description.GroupName.ToUpperInvariant(),
                        Description = description.IsDeprecated ? "This API version is deprecated." : null
                    });
                }
            });

            builder.Services.AddMemoryCache();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();

                    var keyRepo = services.GetRequiredService<IKeyRepository>();
                    var existingKeys = await keyRepo.GetKeysAsync();

                    if (!existingKeys.Any())
                    {
                        var adminKeyStr = Guid.NewGuid().ToString("N");
                        var adminKey = new ApiKey
                        {
                            Key = adminKeyStr,
                            Role = "ADMIN",
                            OwnerName = "Initial Admin"
                        };

                        await keyRepo.CreateKeyAsync(adminKey);

                        Console.WriteLine($"#INITIAL KEY: {adminKeyStr}#");
                        Console.WriteLine($"#ќЅя«ј“≈Ћ№Ќќ создайте новый ключ и удалите начальный#");
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "ќшибка при миграции или создании начального ключа.");
                }
            }

            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

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
