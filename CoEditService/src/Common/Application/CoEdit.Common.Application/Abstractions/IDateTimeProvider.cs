namespace CoEdit.Common.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}