namespace CoEdit.Common.Domain.Exception;

public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}