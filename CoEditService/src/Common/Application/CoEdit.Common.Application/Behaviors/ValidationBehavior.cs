using System.Reflection;

namespace CoEdit.Common.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure != null)
            .GroupBy(
                x => x.PropertyName,
                x => x.ErrorMessage,
                (propertyName, errorMessages) => new
                {
                    Key = propertyName,
                    Values = errorMessages.Distinct().ToArray()
                })
            .ToDictionary(x => x.Key, x => x.Values);

        if (errors.Any())
        {
            if (TryCreateFailureResponse(errors, out var failureResponse))
            {
                return failureResponse;
            }

            throw new InvalidOperationException(
                $"ValidationBehavior expected a Result/Result<T> response type but received '{typeof(TResponse).FullName}' for request '{typeof(TRequest).FullName}'.");
        }

        return await next();
    }

    private static bool TryCreateFailureResponse(
        IReadOnlyDictionary<string, string[]> errors,
        out TResponse response)
    {
        const string message = "Validation failed.";
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure(message, errors);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(Result.Failure),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string), typeof(IReadOnlyDictionary<string, string[]>)],
                modifiers: null);

            if (failureMethod is not null)
            {
                response = (TResponse)failureMethod.Invoke(null, [message, errors])!;
                return true;
            }
        }

        response = default!;
        return false;
    }
}