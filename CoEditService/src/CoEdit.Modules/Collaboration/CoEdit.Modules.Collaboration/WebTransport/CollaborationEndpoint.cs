using System.Text;
using System.Threading.Tasks;

namespace CoEdit.Modules.Collaboration.WebTransport;

public class CollaborationEndpoint : ConnectionHandler
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<CollaborationEndpoint> _logger;
    private readonly DeltaBuffer _deltaBuffer;
    public CollaborationEndpoint(ConnectionManager connectionManager, ILogger<CollaborationEndpoint> logger, DeltaBuffer deltaBuffer)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _deltaBuffer = deltaBuffer;
    }
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var session = new CollaborationSession(context, _connectionManager, _logger, _deltaBuffer);
        _connectionManager.AddSession(session);
        _logger.LogInformation($"Client connected: {connection.ConnectionId}");

        try
        {
            var receiving = ProcessReceiving(connection, session);
            var sending = ProcessSending(connection, session);

            await Task.WhenAll(receiving, sending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in collaboration endpoint");
        }
        finally
        {
            _connectionManager.RemoveSession(session);
            _logger.LogInformation($"Client disconnected: {connection.ConnectionId}");
        }
    }

    private async Task ProcessReceiving(ConnectionContext connection, CollaborationSession session)
    {
        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            try
            {
                if (result.IsCanceled) break;

                while (TryReadMessage(ref buffer, out var message))
                {
                    await ProcessMessageAsync(session, message);
                }

                if (result.IsCompleted) break;
            }
            finally
            {
                connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out string message)
    {
        var position = buffer.PositionOf((byte)'\n');
        if (position == null)
        {
            message = null;
            return false;
        }

        var messageBuffer = buffer.Slice(0, position.Value);
        message = Encoding.UTF8.GetString(messageBuffer);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    private async Task ProcessMessageAsync(CollaborationSession session, string message)
    {
        _logger.LogInformation($"Received message from {session.ConnectionId}: {message}");

        await _deltaBuffer.Writer.WriteAsync(message);

        var response = Encoding.UTF8.GetBytes(message + "\n");

        foreach (var otherSession in _connectionManager.GetAllSessions())
        {
            if (otherSession.ConnectionId != session.ConnectionId)
            {
                await otherSession.Transport.Output.WriteAsync(response);
            }
        }
    }

    private async Task ProcessSending(ConnectionContext connection, CollaborationSession session)
    {
        await foreach (var message in session.Outgoing.ReadAllAsync())
        {
            await connection.Transport.Output.WriteAsync(message);
        }
    }
}