namespace CoEdit.Common.Infrastructure.Resilience;

public static class ResiliencePipelineNames
{
    public const string Redis = "redis-transient";
    public const string S3 = "s3-transient";
}
