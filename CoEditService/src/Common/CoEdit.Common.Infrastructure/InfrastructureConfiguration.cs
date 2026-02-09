using CoEdit.Common.Application.Data;
using CoEdit.Common.Infrastructure.Data;
using CoEdit.Common.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using StackExchange.Redis;

namespace CoEdit.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        string databaseConnectionString,
        string redisConnectionString)
    {
        NpgsqlDataSource npgsqlDataSource = new NpgsqlDataSourceBuilder(databaseConnectionString)
            .Build();

        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        
        services.TryAddSingleton(npgsqlDataSource);
        
        IConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        
        services.TryAddSingleton(connectionMultiplexer);
        
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

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