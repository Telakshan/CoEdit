using System.Diagnostics.CodeAnalysis;
using Collaboration.Application.Services;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Operations;
using Collaboration.Infrastructure.Configuration;
using Collaboration.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Collaboration.Infrastructure;

[ExcludeFromCodeCoverage(Justification = "DI registration for Redis/SignalR; tested via integration test fixtures")]
public static class CollaborationModuleInfrastructure
{
    public static IServiceCollection AddCollaborationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OperationalTransformRolloutOptions>(
            configuration.GetSection(OperationalTransformRolloutOptions.SectionName));
        services.Configure<CursorBroadcastOptions>(
            configuration.GetSection(CursorBroadcastOptions.SectionName));

        var redisConnectionString = configuration["Redis:ConnectionString"]!;

        services.AddSignalR()
            .AddStackExchangeRedis(redisConnectionString);

        services.AddSingleton<IOperationalTransform, TextOperationalTransform>();
        services.AddScoped<IDocumentSessionManager, RedisDocumentSessionManager>();

        services.AddScoped<ISessionStateService, SessionStateService>();
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<IDocumentPersistenceQueue, CoalescedDocumentPersistenceQueue>();
        services.AddHostedService(sp => (CoalescedDocumentPersistenceQueue)sp.GetRequiredService<IDocumentPersistenceQueue>());
        services.AddSingleton<CursorBroadcastScheduler>();
        services.AddSingleton<ICursorBroadcastScheduler>(sp => sp.GetRequiredService<CursorBroadcastScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<CursorBroadcastScheduler>());

        services.AddHostedService<SessionCleanupService>();

        return services;
    }
}