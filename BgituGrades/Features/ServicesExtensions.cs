using AspNetCore.Authentication.ApiKey;
using BgituGrades.Repositories;
using BgituGrades.Services;

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
            return services;
        }
    }
}
