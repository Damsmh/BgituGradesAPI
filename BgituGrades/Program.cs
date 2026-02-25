using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCore.Authentication.ApiKey;
using BgituGrades.Data;
using BgituGrades.Features;
using BgituGrades.Hubs;
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
        public static void Main(string[] args)
        {
            ExcelPackage.License.SetNonCommercialOrganization("BGITU");
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("PostgreSQL")));
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")),
                    ServiceLifetime.Scoped);

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
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
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
