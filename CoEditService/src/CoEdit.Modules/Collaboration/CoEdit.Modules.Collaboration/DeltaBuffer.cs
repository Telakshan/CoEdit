using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace CoEdit.Modules.Collaboration;

public record DocumentSessionData(string DocumentId, List<string> History);

public class DeltaBuffer
{

    private readonly Channel<DocumentSessionData> _channel;

    public DeltaBuffer()
    {
        _channel = Channel.CreateUnbounded<DocumentSessionData>();
    }

    public async ValueTask WriteAsync(DocumentSessionData data)
    {
        await _channel.Writer.WriteAsync(data);
    }

    public async IAsyncEnumerable<DocumentSessionData> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
            yield return item;
    }
}
