using System.Threading.Channels;

namespace CoEdit.Modules.Collaboration;

public class DeltaBuffer
{
    private readonly Channel<string> _channel;

    public DeltaBuffer()
    {
        _channel = Channel.CreateUnbounded<string>();
    }

    public ChannelWriter<string> Writer => _channel.Writer;
    public ChannelReader<string> Reader => _channel.Reader;
}
