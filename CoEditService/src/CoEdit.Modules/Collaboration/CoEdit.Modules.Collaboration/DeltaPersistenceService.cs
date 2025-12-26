using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoEdit.Modules.Collaboration;

public class DeltaPersistenceService : BackgroundService
{
    private readonly ChannelReader<string> _deltaReader;
    private readonly IHistoryService _historyService;
}