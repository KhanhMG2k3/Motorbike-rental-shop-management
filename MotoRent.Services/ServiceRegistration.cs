using Microsoft.Extensions.DependencyInjection;
using MotoRent.DataAccess.Repositories;
using MotoRent.Services.Interfaces;

namespace MotoRent.Services;

/// <summary>
/// Đăng ký DI cho Repository + Service. Ai thêm service mới thì đăng ký ở đây.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddMotoRentServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IMotorbikeRepository, MotorbikeRepository>();

        // Services - Dev 2 (Catalog)
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IMotorbikeService, MotorbikeService>();
        services.AddScoped<IRentalQueryService, RentalQueryService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<ILookupService, LookupService>();

        // Services - Dev 1 (Auth, Users, Branches, Payments, Dashboard): thêm ở đây
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Services - Dev 3 (Rentals, Reviews): thêm ở đây

        return services;
    }
}
