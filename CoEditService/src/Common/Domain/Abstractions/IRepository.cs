namespace CoEdit.Common.Domain.Abstractions;

public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public interface IReadRepository<T> where T : AggregateRoot
{
    // Usually separate read model or IQueryable
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}