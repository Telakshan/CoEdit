using CoEdit.Modules.History;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace CoEdit.Modules.Collaboration;

public class DeltaPersistenceService : BackgroundService
{
    private readonly DeltaBuffer _buffer;
    private readonly IHistoryService _historyService;
    private readonly ILogger<DeltaPersistenceService> _logger;

    public DeltaPersistenceService(
        DeltaBuffer buffer,
        IHistoryService historyService,
        ILogger<DeltaPersistenceService> logger
    )
    {
        _buffer = buffer;
        _historyService = historyService;
        _logger = logger;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sessionData in _buffer.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Saving history for document: {SessionDataDocumentId}...", sessionData.DocumentId);

                await _historyService.SaveHistoryAsync(sessionData.DocumentId, sessionData.History);
            }
        }
    }
}