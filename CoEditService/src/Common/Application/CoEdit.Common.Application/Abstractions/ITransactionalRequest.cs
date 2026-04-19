namespace CoEdit.Common.Application.Abstractions;

public interface ITransactionalRequest
{
    string UnitOfWorkName { get; }
}