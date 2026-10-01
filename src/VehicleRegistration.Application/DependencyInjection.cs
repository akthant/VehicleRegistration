using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VehicleRegistration.Application.Services;
namespace VehicleRegistration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IManufacturerService, ManufacturerService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}