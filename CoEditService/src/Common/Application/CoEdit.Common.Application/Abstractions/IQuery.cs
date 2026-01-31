namespace CoEdit.Common.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}