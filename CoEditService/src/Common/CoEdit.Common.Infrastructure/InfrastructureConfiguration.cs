using CoEdit.Common.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CoEdit.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IEventBus, InMemoryEventBus>();

        return services;
    }

    public static void ConfigureSerilog(this ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();

        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        loggingBuilder.AddSerilog(logger);
    }
}