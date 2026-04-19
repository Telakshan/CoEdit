using CoEdit.Common.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CoEdit.Common.Application.Behaviors;

public class UnitOfWorkPipelineBehavior<TRequest, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest transactionalRequest)
        {
            return await next(cancellationToken);
        }

        var response = await next(cancellationToken);

        if (response is Result result && result.IsFailure)
        {
            return response;
        }

        var unitOfWork = serviceProvider.GetRequiredKeyedService<IUnitOfWork>(transactionalRequest.UnitOfWorkName);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}