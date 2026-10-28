namespace QueueSystem.API.Repositories.Interfaces;

public interface IRepository<T>
    where T : class
{
    Task<List<T>> GetAll(CancellationToken cancellationToken);
    Task<T?> GetById(int id, CancellationToken cancellationToken);
    Task Add(T entity, CancellationToken cancellationToken);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChanges(CancellationToken cancellationToken);
}
