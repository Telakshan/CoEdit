using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CoEdit.Modules.Collaboration;

public record DocumentSessionData(string DocumentId, List<string> History);
public class DeltaBuffer
{
    private readonly Channel<DocumentSessionData> _channel;

    public DeltaBuffer()
    {
        _channel = Channel.CreateUnbounded<DocumentSessionData>();
    }

    public async ValueTask WriteAsync(DocumentSessionData documentSessionData)
    {
        await _channel.Writer.WriteAsync(documentSessionData);
    }

    public IAsyncEnumerable<DocumentSessionData> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
