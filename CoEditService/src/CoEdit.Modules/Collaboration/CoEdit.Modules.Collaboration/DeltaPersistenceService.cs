using CoEdit.Modules.History;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

namespace CoEdit.Modules.Collaboration;

public class DeltaPersistenceService : BackgroundService
{
    private readonly ChannelReader<string> _deltaReader;
    private readonly IHistoryService _historyService;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}