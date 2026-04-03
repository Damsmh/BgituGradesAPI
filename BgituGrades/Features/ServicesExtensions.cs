using Asp.Versioning.ApiExplorer;
using AspNetCore.Authentication.ApiKey;
using BgituGrades.Repositories;
using BgituGrades.Services;
using BgituGrades.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BgituGrades.Features
{
    public static class ServicesExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDisciplineRepository, DisciplineRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IClassRepository, ClassRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IWorkRepository, WorkRepository>();
            services.AddScoped<IMarkRepository, MarkRepository>();
            services.AddScoped<IPresenceRepository, PresenceRepository>();
            services.AddScoped<ITransferRepository, TransferRepository>();
            services.AddScoped<IKeyRepository, KeyRepository>();
            services.AddScoped<ISettingRepository, SettingRepository>();
            services.AddScoped<IReportSnapshotRepository, ReportSnapshotRepository>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IDisciplineService, DisciplineService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IWorkService, WorkService>();
            services.AddScoped<IMarkService, MarkService>();
            services.AddScoped<IPresenceService, PresenceService>();
            services.AddScoped<ITransferService, TransferService>();
            services.AddScoped<IMigrationService, MigrationsService>();
            services.AddScoped<IApiKeyProvider, ApiKeyProvider>();
            services.AddScoped<IKeyService, KeyService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IArchivedReportService, ArchivedReportService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<IAuthorizationHandler, GroupAccessHandler>();
            services.AddScoped<IScheduleLoaderService, ScheduleLoaderService>();
            services.ConfigureOptions<ConfigureSwaggerOptions>();

            services.AddSingleton<ITokenHasher, TokenHasher>();
            return services;
        }


        public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<CreateClassRequestValidator>();
            return services;
        }
    }

    public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
    {
        public void Configure(SwaggerGenOptions options)
        {
            options.DescribeAllParametersInCamelCase();
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = "BGITU.GRADES API",
                    Version = description.GroupName.ToUpperInvariant(),
                    Description = description.IsDeprecated ? "This API version is deprecated." : null
                });
            }
        }
    }
}
