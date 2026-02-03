using Microsoft.Extensions.Hosting;
using CoEdit.Modules.History;
using Microsoft.Extensions.Logging;

namespace CoEdit.Modules.Collaboration;

public class DeltaPersistenceService(
    DeltaBuffer deltaBuffer,
    IHistoryRepository historyRepository,
    ILogger<DeltaPersistenceService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sessionData in deltaBuffer.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation($"Saving history for Document {sessionData.DocumentId}");

                await historyRepository.SaveHistoryAsync(sessionData.DocumentId, sessionData.History);

                logger.LogInformation($"Successfully saved document {sessionData.DocumentId}");
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, $"Error saving history for Document {sessionData.DocumentId}");
            }
        }
    }
}