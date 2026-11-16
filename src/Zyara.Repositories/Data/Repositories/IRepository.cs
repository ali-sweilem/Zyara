using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Zyara.Repositories.Data.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();
    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

public sealed class EfRepository<TEntity>(ZyaraDbContext db) : IRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _set = db.Set<TEntity>();

    public IQueryable<TEntity> Query() => _set;

    public ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default) =>
        _set.FindAsync(keyValues, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(predicate, cancellationToken);

    public void Add(TEntity entity) => _set.Add(entity);
    public void Update(TEntity entity) => _set.Update(entity);
    public void Remove(TEntity entity) => _set.Remove(entity);
}


