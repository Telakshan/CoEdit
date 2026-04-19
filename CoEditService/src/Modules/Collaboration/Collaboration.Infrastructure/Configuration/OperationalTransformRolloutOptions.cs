namespace Collaboration.Infrastructure.Configuration;

public sealed class OperationalTransformRolloutOptions
{
    public const string SectionName = "Collaboration:OperationalTransform";

    public bool Enabled { get; set; }
}