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

        // Services - Dev 1 (Auth, Users, Branches, Payments, Dashboard): thêm ở đây
        // Services - Dev 3 (Rentals, Reviews): thêm ở đây

        return services;
    }
}
