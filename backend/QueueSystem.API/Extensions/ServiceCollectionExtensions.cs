using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Data;
using QueueSystem.API.Repositories;
using QueueSystem.API.Repositories.Interfaces;
using QueueSystem.API.Services;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ITicketRepository, TicketRepository>();

        services.AddScoped<IServiceTypeService, ServiceTypeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IManagerDepartmentService, ManagerDepartmentService>();
        services.AddScoped<ICounterService, CounterService>();
        services.AddScoped<IOperatorService, OperatorService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IServiceLoadService, ServiceLoadService>();
        services.AddScoped<IManagerStatisticsService, ManagerStatisticsService>();
        services.AddScoped<IDisplayService, DisplayService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
