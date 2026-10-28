using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Data;
using QueueSystem.API.Repositories.Interfaces;

namespace QueueSystem.API.Repositories;

public class Repository<T> : IRepository<T>
    where T : class
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<T>();
    }

    public virtual async Task<List<T>> GetAll(CancellationToken cancellationToken)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetById(int id, CancellationToken cancellationToken)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task Add(T entity, CancellationToken cancellationToken)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<int> SaveChanges(CancellationToken cancellationToken)
    {
        return await DbContext.SaveChangesAsync(cancellationToken);
    }
}
