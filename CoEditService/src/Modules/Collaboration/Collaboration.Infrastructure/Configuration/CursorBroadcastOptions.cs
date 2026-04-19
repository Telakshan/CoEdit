namespace Collaboration.Infrastructure.Configuration;

public sealed class CursorBroadcastOptions
{
    public const string SectionName = "Collaboration:CursorBroadcast";

    public int IntervalMs { get; set; } = 40;

    public int GetNormalizedIntervalMs()
    {
        return Math.Clamp(IntervalMs, 10, 250);
    }
}