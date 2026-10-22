using AutoMapper;
using Clinic.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Application;

public static class DependencyInjection
{
    /// <summary>Registers MediatR (with the FluentValidation pipeline), validators, and AutoMapper.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // AutoMapper configured manually to avoid an extra DI extension package.
        // Profiles live alongside the feature handlers and are discovered via AddMaps.
        services.AddSingleton<IMapper>(_ =>
            new MapperConfiguration(c => c.AddMaps(typeof(DependencyInjection).Assembly)).CreateMapper());

        return services;
    }
}
