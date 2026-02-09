using Microsoft.EntityFrameworkCore;

namespace CoEdit.Common.Infrastructure;

public abstract class Repository<T>(DbContext dbContext) : IRepository<T>
    where T : AggregateRoot
{
    private readonly DbContext _dbContext = dbContext;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public void Add(T entity)
    {
        _dbContext.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        _dbContext.Set<T>().Update(entity);
    }

    public void Remove(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
    }
}