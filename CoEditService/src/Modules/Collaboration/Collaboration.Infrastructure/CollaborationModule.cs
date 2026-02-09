using Collaboration.Application.Services;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Operations;
using Collaboration.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Collaboration.Infrastructure;

public static class CollaborationModule
{
    public static IServiceCollection AddCollaborationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCollaborationInfrastructure(configuration);
        
        return services;
    }

    private static void AddCollaborationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR()
            .AddStackExchangeRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost:6379"));

        services.AddSingleton<IOperationalTransform, TextOperationalTransform>();
        //services.AddScoped<IDocumentSessionManager, RedisDocumentSessionManager>();

        services.AddScoped<ISessionStateService, SessionStateService>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        
        services.AddHostedService<SessionCleanupService>();
    }
}