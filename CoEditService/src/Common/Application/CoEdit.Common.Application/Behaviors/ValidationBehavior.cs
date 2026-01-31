using System.Reflection;

namespace CoEdit.Common.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .ToList();

        if (failures.Count == 0) return await next(cancellationToken);
        var error = Error.Validation("Validation.Error", failures.First().ErrorMessage);

        // If TResponse is a generic Result<T>, we need to create it using reflection or dynamic
        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse);
            var method = resultType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static,
                new[] { typeof(Error) });

            if (method != null) return (TResponse)method.Invoke(null, [error])!;
        }
        else if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        throw new ValidationException(failures);
    }
}