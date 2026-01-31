using CoEdit.Common.Infrastructure.Data;
using Microsoft.Extensions.Hosting;

namespace CoEdit.Common.Infrastructure.Messaging;

public class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : BaseDbContext
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}