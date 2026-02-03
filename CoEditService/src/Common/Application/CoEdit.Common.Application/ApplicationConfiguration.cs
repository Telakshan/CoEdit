using System.Reflection;
using CoEdit.Common.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace CoEdit.Common.Application;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(assemblies);

            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        foreach (var assembly in assemblies) services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}