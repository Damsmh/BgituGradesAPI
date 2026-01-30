using AspNetCore.Authentication.ApiKey;
using BgutuGrades.Repositories;
using BgutuGrades.Services;

namespace BgutuGrades.Features
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
            services.AddScoped<IApiKeyProvider, ApiKeyProvider>();
            return services;
        }
    }
}
